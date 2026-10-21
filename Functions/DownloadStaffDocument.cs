
using System.Net;
using Azure;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChill.Functions.Functions;

public class DownloadStaffDocument
{
    private readonly StorageService _storage;

    public DownloadStaffDocument(StorageService storage)
    {
        _storage = storage;
    }

    [Function("DownloadStaffDocument")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get",
            Route = "documents/download/{fileName}")]
        HttpRequestData req,
        string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName.Contains('/') ||
            fileName.Contains('\\') ||
            fileName == "." ||
            fileName == "..")
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new
            {
                error = "Invalid file name."
            });
            return bad;
        }

        var container =
            await _storage.GetStaffDocumentsContainerAsync();

        var blob = container.GetBlobClient(fileName);

        try
        {
            var download = await blob.DownloadStreamingAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add(
                "Content-Type",
                download.Value.Details.ContentType
                    ?? "application/octet-stream");

            response.Headers.Add(
                "Content-Disposition",
                $"attachment; filename=\"{Path.GetFileName(fileName).Replace("\"", "")}\"");

            await download.Value.Content.CopyToAsync(response.Body);
            return response;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new
            {
                error = "Document not found."
            });
            return notFound;
        }
    }
}
