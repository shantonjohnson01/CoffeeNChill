
using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Api.Models;

namespace CoffeeNChill.Api.Services;

public class UserTableService
{
    private readonly TableClient _table;

    public UserTableService(IConfiguration configuration)
    {
        string connectionString =
            configuration["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException(
                "AzureWebJobsStorage is not configured.");

        _table = new TableServiceClient(connectionString)
            .GetTableClient("Users");
    }

    public async Task InitializeAsync()
    {
        await _table.CreateIfNotExistsAsync();
    }

    public static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    public async Task<UserEntity?> FindByEmailAsync(string email)
    {
        string normalized = NormalizeEmail(email);

        // Role is the partition key, so search each allowed role.
        foreach (string role in new[] { "Admin", "Staff", "Customer" })
        {
            try
            {
                var result = await _table.GetEntityAsync<UserEntity>(
                    role, normalized);

                return result.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // The user isn't in this role partition; try the next one.
            }
        }

        return null;
    }

    public async Task<UserEntity?> FindByRoleAndEmailAsync(
        string role, string email)
    {
        try
        {
            var result = await _table.GetEntityAsync<UserEntity>(
                role, NormalizeEmail(email));

            return result.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task CreateAsync(UserEntity user)
    {
        user.RowKey = NormalizeEmail(user.RowKey);
        await _table.AddEntityAsync(user);
    }
}
