
using System.Net;
using Azure;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChill.Functions.Functions;

public class DeleteStaffDocument
{
    private readonly StorageService _storage;

    public DeleteStaffDocument(StorageService storage)
    {
        _storage = storage;
    }

    [Function("DeleteStaffDocument")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete",
            Route = "documents/{fileName}")] HttpRequestData req,
        string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName.Contains('/') ||
            fileName.Contains('\\') ||
            fileName is "." or "..")
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new { error = "Invalid file name." });
            return bad;
        }

        var container =
            await _storage.GetStaffDocumentsContainerAsync();

        var blob = container.GetBlobClient(fileName);

        try
        {
            var result = await blob.DeleteIfExistsAsync();

            if (!result.Value)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteAsJsonAsync(
                    new { error = "Document not found." });
                return notFound;
            }

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(
                new { error = "Document not found." });
            return notFound;
        }
    }
}
