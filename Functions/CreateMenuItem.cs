
using System.Net;
using System.Text.RegularExpressions;
using Azure;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChill.Functions.Functions;

public class CreateMenuItem
{
    private readonly StorageService _storage;

    public CreateMenuItem(StorageService storage)
    {
        _storage = storage;
    }

    [Function("CreateMenuItem")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post",
            Route = "menu")] HttpRequestData req)
    {
        var input = await req.ReadFromJsonAsync<CreateMenuItemRequest>();

        if (input is null ||
            string.IsNullOrWhiteSpace(input.Category) ||
            string.IsNullOrWhiteSpace(input.Id) ||
            string.IsNullOrWhiteSpace(input.Name) ||
            string.IsNullOrWhiteSpace(input.Description) ||
            input.Price < 0 ||
            double.IsNaN(input.Price) ||
            double.IsInfinity(input.Price))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new
            {
                error = "Category, ID, name and description are required. Price must be a valid non-negative number."
            });
            return badRequest;
        }

        string category = input.Category.Trim();
        string id = input.Id.Trim();

        if (category.Contains('/') ||
            id.Contains('/') ||
            id.Contains('\\') ||
            Regex.IsMatch(category, @"[\x00-\x1F]") ||
            Regex.IsMatch(id, @"[\x00-\x1F]"))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new
            {
                error = "Category and ID contain invalid characters."
            });
            return badRequest;
        }

        var entity = new MenuItemEntity
        {
            PartitionKey = category,
            RowKey = id,
            Name = input.Name.Trim(),
            Description = input.Description.Trim(),
            Price = input.Price,
            IsAvailable = input.IsAvailable
        };

        var table = await _storage.GetMenuTableAsync();

        try
        {
            await table.AddEntityAsync(entity);
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            var conflict = req.CreateResponse(HttpStatusCode.Conflict);
            await conflict.WriteAsJsonAsync(new
            {
                error = "An item with this ID already exists in this category."
            });
            return conflict;
        }

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(new
        {
            message = "Menu item created successfully.",
            item = entity
        });

        return response;
    }
}

public class CreateMenuItemRequest
{
    public string Category { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Price { get; set; }
    public bool IsAvailable { get; set; } = true;
}
