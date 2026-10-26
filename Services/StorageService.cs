
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Configuration;

namespace CoffeeNChill.Functions.Services;

public class StorageService
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly QueueServiceClient _queueServiceClient;

    public StorageService(IConfiguration configuration)
    {
        string connectionString =
            configuration["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException(
                "AzureWebJobsStorage is not configured.");

        _tableServiceClient = new TableServiceClient(connectionString);
        _blobServiceClient = new BlobServiceClient(connectionString);
        _queueServiceClient = new QueueServiceClient(connectionString);
    }

    public async Task<TableClient> GetMenuTableAsync()
    {
        TableClient table = _tableServiceClient.GetTableClient("MenuItems");
        await table.CreateIfNotExistsAsync();
        return table;
    }

    public async Task<BlobContainerClient> GetStaffDocumentsContainerAsync()
    {
        BlobContainerClient container =
            _blobServiceClient.GetBlobContainerClient("staff-docs");

        await container.CreateIfNotExistsAsync();
        return container;
    }

    public async Task<QueueClient> GetOrderQueueAsync()
    {
        QueueClient queue = _queueServiceClient.GetQueueClient(
            "order-processing-queue");

        await queue.CreateIfNotExistsAsync();
        return queue;
    }

    public async Task<TableClient> GetOrdersTableAsync()
    {
        TableClient table = _tableServiceClient.GetTableClient("Orders");
        await table.CreateIfNotExistsAsync();
        return table;
    }
}
