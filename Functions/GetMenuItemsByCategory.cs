
using System.Net;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChill.Functions.Functions;

public class GetMenuItemsByCategory
{
    private readonly StorageService _storage;

    public GetMenuItemsByCategory(StorageService storage)
    {
        _storage = storage;
    }

    [Function("GetMenuItemsByCategory")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get",
            Route = "menu/category/{category}")] HttpRequestData req,
        string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new
            {
                error = "Category is required."
            });
            return badRequest;
        }

        var table = await _storage.GetMenuTableAsync();
        var items = new List<MenuItemEntity>();

        await foreach (var item in table.QueryAsync<MenuItemEntity>(
            x => x.PartitionKey == category))
        {
            items.Add(item);
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(items);
        return response;
    }
}
