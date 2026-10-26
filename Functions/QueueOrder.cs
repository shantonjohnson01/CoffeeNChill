
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace CoffeeNChill.Functions.Functions;

public class QueueOrder
{
    private readonly StorageService _storage;
    private readonly ILogger<QueueOrder> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public QueueOrder(
        StorageService storage,
        ILogger<QueueOrder> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    [Function("QueueOrder")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post",
            Route = "orders/queue")] HttpRequestData req)
    {
        OrderRequest? input;

        try
        {
            input = await req.ReadFromJsonAsync<OrderRequest>();
        }
        catch (JsonException)
        {
            input = null;
        }

        if (input is null ||
            string.IsNullOrWhiteSpace(input.CustomerName) ||
            input.SelectedItemSKUs is null ||
            input.SelectedItemSKUs.Count == 0 ||
            input.SelectedItemSKUs.Any(string.IsNullOrWhiteSpace) ||
            input.TotalPrice < 0 ||
            double.IsNaN(input.TotalPrice) ||
            double.IsInfinity(input.TotalPrice))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new
            {
                error = "Provide CustomerName, at least one selected SKU, and a valid non-negative TotalPrice."
            });
            return bad;
        }

        string orderId = string.IsNullOrWhiteSpace(input.OrderId)
            ? $"ORD-{Guid.NewGuid():N}"
            : input.OrderId.Trim();

        if (orderId.Length > 100 ||
            orderId.Contains('/') ||
            orderId.Contains('\\') ||
            orderId.Contains('#') ||
            orderId.Contains('?'))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new
            {
                error = "OrderId is invalid or too long."
            });
            return bad;
        }

        var order = new OrderMessage
        {
            OrderId = orderId,
            CustomerName = input.CustomerName.Trim(),
            SelectedItemSKUs = input.SelectedItemSKUs
                .Select(sku => sku.Trim())
                .ToList(),
            TotalPrice = input.TotalPrice,
            OrderTimestamp = input.OrderTimestamp ?? DateTimeOffset.UtcNow
        };

        string json = JsonSerializer.Serialize(order, JsonOptions);

        try
        {
            QueueClient queue = await _storage.GetOrderQueueAsync();

            // Explicitly use Base64 so this message matches the
            // queue message encoding configured in host.json.
            await queue.SendMessageAsync(
                Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(json)));

            _logger.LogInformation(
                "Queued order {OrderId} for customer {CustomerName}",
                order.OrderId,
                order.CustomerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue order {OrderId}", orderId);

            var unavailable = req.CreateResponse(
                HttpStatusCode.ServiceUnavailable);

            await unavailable.WriteAsJsonAsync(new
            {
                error = "The order queue is temporarily unavailable."
            });

            return unavailable;
        }

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new
        {
            message = "Order accepted for asynchronous processing.",
            orderId = order.OrderId,
            status = "Queued",
            queue = "order-processing-queue"
        });

        return response;
    }
}
