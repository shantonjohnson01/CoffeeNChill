
using System.Net;
using Azure;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChill.Functions.Functions;

public class GetOrderStatus
{
    private readonly StorageService _storage;

    public GetOrderStatus(StorageService storage)
    {
        _storage = storage;
    }

    [Function("GetOrderStatus")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get",
            Route = "orders/{orderId}")] HttpRequestData req,
        string orderId)
    {
        var table = await _storage.GetOrdersTableAsync();

        // RowKey alone is not unique across all partitions, so query
        // by order ID and return the matching order.
        var matches = new List<OrderEntity>();

        await foreach (var item in table.QueryAsync<OrderEntity>(
            x => x.RowKey == orderId))
        {
            matches.Add(item);
        }

        if (matches.Count == 0)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new
            {
                error = "Order not found. It may still be waiting in the queue."
            });
            return notFound;
        }

        var order = matches[0];

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            orderId = order.RowKey,
            customerName = order.CustomerName,
            selectedItemSKUs = order.SelectedItemSKUsJson,
            totalPrice = order.TotalPrice,
            orderTimestamp = order.OrderTimestamp,
            status = order.Status,
            statusUpdatedAt = order.StatusUpdatedAt
        });

        return response;
    }
}
