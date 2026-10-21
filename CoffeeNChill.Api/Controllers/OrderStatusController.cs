
using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Api.Controllers;

/// <summary>Updates order status for authenticated Staff and Admin users.</summary>
[ApiController]
[Route("api/orders/status")]
[Authorize(Roles = "Admin,Staff")]
public class OrderStatusController : ControllerBase
{
    private readonly TableClient _orders;

    public OrderStatusController(IConfiguration configuration)
    {
        string connectionString =
            configuration["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException(
                "AzureWebJobsStorage is not configured.");

        _orders = new TableServiceClient(connectionString)
            .GetTableClient("Orders");
    }

    /// <summary>Updates an existing order's preparation status.</summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        [FromBody] OrderStatusUpdateRequest request)
    {
        if (!DateTime.TryParseExact(
                request.OrderDate,
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out _))
        {
            return BadRequest(new
            {
                error = "OrderDate must use yyyy-MM-dd format."
            });
        }

        try
        {
            var result = await _orders.GetEntityAsync<TableEntity>(
                request.OrderDate, request.OrderId);

            TableEntity entity = result.Value;
            entity["Status"] = request.Status;
            entity["StatusUpdatedAt"] = DateTimeOffset.UtcNow;

            await _orders.UpdateEntityAsync(
                entity, ETag.All, TableUpdateMode.Merge);

            return Ok(new
            {
                orderId = request.OrderId,
                orderDate = request.OrderDate,
                status = request.Status,
                statusUpdatedAt = DateTimeOffset.UtcNow
            });
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return NotFound(new
            {
                error = "Order not found. Check OrderDate and OrderId."
            });
        }
    }
}
