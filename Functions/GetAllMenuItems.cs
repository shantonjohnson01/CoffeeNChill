
using System.Net;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChill.Functions.Functions;

public class GetAllMenuItems
{
    private readonly StorageService _storage;

    public GetAllMenuItems(StorageService storage)
    {
        _storage = storage;
    }

    [Function("GetAllMenuItems")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get",
            Route = "menu")] HttpRequestData req)
    {
        var table = await _storage.GetMenuTableAsync();
        var items = new List<MenuItemEntity>();

        await foreach (var item in table.QueryAsync<MenuItemEntity>())
        {
            items.Add(item);
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(items);
        return response;
    }
}
