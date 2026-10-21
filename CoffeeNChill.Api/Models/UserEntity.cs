
using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Api.Models;

public class UserEntity : ITableEntity
{
    // Role: Admin, Staff, or Customer
    public string PartitionKey { get; set; } = "Customer";

    // Normalized email address
    public string RowKey { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }
}
