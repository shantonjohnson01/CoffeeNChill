
using System.Net.Http.Headers;
using CoffeeNChill.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Api.Controllers;

/// <summary>
/// Proxies permitted menu, document, and order-queue requests
/// to the Azure Functions application.
/// </summary>
[ApiController]
[Route("api")]
public class GatewayController : ControllerBase
{
    private readonly FunctionsProxyService _proxy;

    public GatewayController(FunctionsProxyService proxy)
    {
        _proxy = proxy;
    }

    private async Task<IActionResult> Forward(string downstreamPath)
    {
        using var downstream =
            await _proxy.ForwardAsync(HttpContext, downstreamPath);

        Response.StatusCode = (int)downstream.StatusCode;

        if (downstream.Content.Headers.ContentType is not null)
        {
            Response.ContentType =
                downstream.Content.Headers.ContentType.ToString();
        }

        if (downstream.Content.Headers.ContentDisposition is not null)
        {
            Response.Headers.ContentDisposition =
                downstream.Content.Headers.ContentDisposition.ToString();
        }

        await downstream.Content.CopyToAsync(
            Response.Body, HttpContext.RequestAborted);

        return new EmptyResult();
    }

    /// <summary>Returns all menu items without requiring login.</summary>
    [AllowAnonymous]
    [HttpGet("menu")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> GetMenu() =>
        Forward("/api/menu");

    /// <summary>Returns menu items in the specified category.</summary>
    [AllowAnonymous]
    [HttpGet("menu/category/{category}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> GetMenuByCategory(string category) =>
        Forward($"/api/menu/category/{Uri.EscapeDataString(category)}");

    /// <summary>Creates a menu item. Staff or Admin required.</summary>
    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("menu")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> CreateMenuItem() =>
        Forward("/api/menu");

    /// <summary>Updates a menu item. Staff or Admin required.</summary>
    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("menu/{category}/{id}")]
    public Task<IActionResult> UpdateMenuItem(
        string category, string id) =>
        Forward(
            $"/api/menu/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(id)}");

    /// <summary>Deletes a menu item. Staff or Admin required.</summary>
    [Authorize(Roles = "Admin,Staff")]
    [HttpDelete("menu/{category}/{id}")]
    public Task<IActionResult> DeleteMenuItem(
        string category, string id) =>
        Forward(
            $"/api/menu/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(id)}");

    /// <summary>Accepts an order for asynchronous processing.</summary>
    [AllowAnonymous]
    [HttpPost("orders/queue")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public Task<IActionResult> QueueOrder() =>
        Forward("/api/orders/queue");

    /// <summary>Uploads a staff document. Staff or Admin required.</summary>
    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("documents/upload")]
    public Task<IActionResult> UploadDocument() =>
        Forward("/api/documents/upload");

    /// <summary>Lists staff documents. Staff or Admin required.</summary>
    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("documents")]
    public Task<IActionResult> ListDocuments() =>
        Forward("/api/documents");

    /// <summary>Downloads a staff document. Staff or Admin required.</summary>
    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("documents/download/{fileName}")]
    public Task<IActionResult> DownloadDocument(string fileName) =>
        Forward($"/api/documents/download/{Uri.EscapeDataString(fileName)}");

    /// <summary>Deletes a staff document. Staff or Admin required.</summary>
    [Authorize(Roles = "Admin,Staff")]
    [HttpDelete("documents/{fileName}")]
    public Task<IActionResult> DeleteDocument(string fileName) =>
        Forward($"/api/documents/{Uri.EscapeDataString(fileName)}");
}
