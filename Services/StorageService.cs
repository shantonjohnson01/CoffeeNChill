
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Configuration;

namespace CoffeeNChill.Functions.Services;

public class StorageService
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly BlobServiceClient _blobServiceClient;

    public StorageService(IConfiguration configuration)
    {
        string connectionString =
            configuration["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException(
                "AzureWebJobsStorage is not configured.");

        _tableServiceClient = new TableServiceClient(connectionString);
        _blobServiceClient = new BlobServiceClient(connectionString);
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
}
