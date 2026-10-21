
using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace CoffeeNChill.Functions.Functions;

public class UpdateMenuItem
{
    private readonly StorageService _storage;

    public UpdateMenuItem(StorageService storage)
    {
        _storage = storage;
    }

    [Function("UpdateMenuItem")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put",
            Route = "menu/{category}/{id}")] HttpRequestData req,
        string category,
        string id)
    {
        var input = await req.ReadFromJsonAsync<UpdateMenuItemRequest>();

        if (input is null ||
            (input.Price is null && input.IsAvailable is null))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new
            {
                error = "Provide Price, IsAvailable, or both."
            });
            return badRequest;
        }

        if (input.Price is not null &&
            (input.Price < 0 || double.IsNaN(input.Price.Value) ||
             double.IsInfinity(input.Price.Value)))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new
            {
                error = "Price must be a valid non-negative number."
            });
            return badRequest;
        }

        var table = await _storage.GetMenuTableAsync();
        MenuItemEntity entity;

        try
        {
            var result = await table.GetEntityAsync<MenuItemEntity>(
                category, id);
            entity = result.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new
            {
                error = "Menu item not found."
            });
            return notFound;
        }

        if (input.Price.HasValue)
            entity.Price = input.Price.Value;

        if (input.IsAvailable.HasValue)
            entity.IsAvailable = input.IsAvailable.Value;

        await table.UpdateEntityAsync(
            entity, entity.ETag, TableUpdateMode.Replace);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            message = "Menu item updated successfully.",
            item = entity
        });

        return response;
    }
}

public class UpdateMenuItemRequest
{
    public double? Price { get; set; }
    public bool? IsAvailable { get; set; }
}
