
using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Functions.Models;

public class OrderEntity : ITableEntity
{
    // Date of order in yyyy-MM-dd format
    public string PartitionKey { get; set; } = string.Empty;

    // Unique order ID
    public string RowKey { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    // Stored as a JSON string in Table Storage
    public string SelectedItemSKUsJson { get; set; } = "[]";

    public double TotalPrice { get; set; }

    public DateTimeOffset OrderTimestamp { get; set; }

    public string Status { get; set; } = "Received";

    public DateTimeOffset StatusUpdatedAt { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }
}
