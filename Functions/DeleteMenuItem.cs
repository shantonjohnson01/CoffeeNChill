
using System.Net;
using Azure;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChill.Functions.Functions;

public class DeleteMenuItem
{
    private readonly StorageService _storage;

    public DeleteMenuItem(StorageService storage)
    {
        _storage = storage;
    }

    [Function("DeleteMenuItem")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete",
            Route = "menu/{category}/{id}")] HttpRequestData req,
        string category,
        string id)
    {
        var table = await _storage.GetMenuTableAsync();

        try
        {
            await table.DeleteEntityAsync(category, id);
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

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            message = "Menu item deleted successfully.",
            category,
            id
        });

        return response;
    }
}
