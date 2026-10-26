
using Azure;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoffeeNChill.Functions.Functions;

public class ProcessOrderQueue
{
    private readonly StorageService _storage;
    private readonly ILogger<ProcessOrderQueue> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public ProcessOrderQueue(
        StorageService storage,
        ILogger<ProcessOrderQueue> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    [Function("ProcessOrderQueue")]
    public async Task Run(
        [QueueTrigger("order-processing-queue",
            Connection = "AzureWebJobsStorage")] string message)
    {
        OrderMessage? order;

        try
        {
            order = JsonSerializer.Deserialize<OrderMessage>(
                message, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Queue message is not valid JSON.");
            throw;
        }

        if (order is null ||
            string.IsNullOrWhiteSpace(order.OrderId) ||
            string.IsNullOrWhiteSpace(order.CustomerName) ||
            order.SelectedItemSKUs is null ||
            order.SelectedItemSKUs.Count == 0 ||
            order.TotalPrice < 0 ||
            double.IsNaN(order.TotalPrice) ||
            double.IsInfinity(order.TotalPrice))
        {
            _logger.LogError(
                "Queue message is missing required order information.");
            throw new InvalidOperationException(
                "Invalid order message. The queue message will be retried.");
        }

        var table = await _storage.GetOrdersTableAsync();

        string orderDate = order.OrderTimestamp
            .UtcDateTime.ToString("yyyy-MM-dd");

        var entity = new OrderEntity
        {
            PartitionKey = orderDate,
            RowKey = order.OrderId,
            CustomerName = order.CustomerName,
            SelectedItemSKUsJson =
                JsonSerializer.Serialize(order.SelectedItemSKUs),
            TotalPrice = order.TotalPrice,
            OrderTimestamp = order.OrderTimestamp,
            Status = "Received",
            StatusUpdatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            // If this is a retried message for an order already stored,
            // don't reset an order that has already progressed.
            try
            {
                var existing = await table.GetEntityAsync<OrderEntity>(
                    orderDate, order.OrderId);

                _logger.LogWarning(
                    "Order {OrderId} already exists with status {Status}.",
                    order.OrderId, existing.Value.Status);

                return;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // The order is new; continue below.
            }

            await table.AddEntityAsync(entity);

            _logger.LogInformation(
                "Order {OrderId} status: Received",
                order.OrderId);

            await ChangeStatusAsync(table, entity, "Preparing");
            await Task.Delay(TimeSpan.FromSeconds(3));

            await ChangeStatusAsync(table, entity, "Ready");
            await Task.Delay(TimeSpan.FromSeconds(3));

            await ChangeStatusAsync(table, entity, "Collected");

            _logger.LogInformation(
                "Order {OrderId} completed its processing lifecycle.",
                order.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "Processing failed for order {OrderId}.", order.OrderId);

            // Re-throw so the Functions queue extension can retry the
            // message and send it to the poison queue after max attempts.
            throw;
        }
    }

    private static async Task ChangeStatusAsync(
        Azure.Data.Tables.TableClient table,
        OrderEntity entity,
        string newStatus)
    {
        entity.Status = newStatus;
        entity.StatusUpdatedAt = DateTimeOffset.UtcNow;

        await table.UpdateEntityAsync(
            entity, ETag.All,
            Azure.Data.Tables.TableUpdateMode.Replace);
    }
}
