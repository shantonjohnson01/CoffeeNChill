
using System.Net;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Net.Http.Headers;

namespace CoffeeNChill.Functions.Functions;

public class UploadStaffDocument
{
    private readonly StorageService _storage;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".txt",
            ".png", ".jpg", ".jpeg"
        };

    public UploadStaffDocument(StorageService storage)
    {
        _storage = storage;
    }

    [Function("UploadStaffDocument")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post",
            Route = "documents/upload")] HttpRequestData req)
    {
        if (!req.Headers.TryGetValues("Content-Type", out var contentTypes))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new
            {
                error = "Content-Type must be multipart/form-data."
            });
            return bad;
        }

        string contentType = contentTypes.FirstOrDefault() ?? "";

        if (!MediaTypeHeaderValue.TryParse(contentType, out var mediaType) ||
            !string.Equals(mediaType.MediaType.Value,
                "multipart/form-data", StringComparison.OrdinalIgnoreCase) ||
            !mediaType.Boundary.HasValue)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new
            {
                error = "Send the file as multipart/form-data."
            });
            return bad;
        }

        string boundary = HeaderUtilities.RemoveQuotes(
            mediaType.Boundary).Value ?? "";

        if (string.IsNullOrWhiteSpace(boundary) || boundary.Length > 200)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new
            {
                error = "Invalid multipart boundary."
            });
            return bad;
        }

        var reader = new MultipartReader(boundary, req.Body);
        BlobContainerClient container =
            await _storage.GetStaffDocumentsContainerAsync();

        MultipartSection? section;

        while ((section = await reader.ReadNextSectionAsync()) is not null)
        {
            if (!ContentDispositionHeaderValue.TryParse(
                    section.ContentDisposition, out var disposition))
            {
                continue;
            }

            bool isFile =
                disposition.FileName.HasValue ||
                disposition.FileNameStar.HasValue;

            if (!isFile)
                continue;

            string rawFileName = disposition.FileNameStar.HasValue
                ? disposition.FileNameStar.Value!
                : disposition.FileName.Value!;

            string fileName = Path.GetFileName(
                HeaderUtilities.RemoveQuotes(rawFileName).Value ?? "");

            if (string.IsNullOrWhiteSpace(fileName))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteAsJsonAsync(new
                {
                    error = "The uploaded file must have a file name."
                });
                return bad;
            }

            string extension = Path.GetExtension(fileName);

            if (!AllowedExtensions.Contains(extension))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteAsJsonAsync(new
                {
                    error = "Unsupported file type. Allowed: PDF, DOC, DOCX, TXT, PNG, JPG and JPEG."
                });
                return bad;
            }

            string blobName = $"{Guid.NewGuid():N}_{fileName}";
            BlobClient blob = container.GetBlobClient(blobName);

            string uploadedContentType =
                section.Headers.TryGetValue("Content-Type", out var values)
                    ? values.ToString()
                    : "application/octet-stream";

            await blob.UploadAsync(
                section.Body,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = uploadedContentType
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        ["originalfilename"] = fileName
                    }
                });

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new
            {
                message = "Document uploaded successfully.",
                fileName = blobName,
                originalFileName = fileName,
                contentType = uploadedContentType
            });

            return response;
        }

        var noFile = req.CreateResponse(HttpStatusCode.BadRequest);
        await noFile.WriteAsJsonAsync(new
        {
            error = "No file was found in the multipart request."
        });

        return noFile;
    }
}
