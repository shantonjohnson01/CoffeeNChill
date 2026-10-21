
using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Api.DTOs;

public class OrderStatusUpdateRequest
{
    [Required]
    public string OrderDate { get; set; } = string.Empty;

    [Required]
    public string OrderId { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Preparing|Ready|Collected)$",
        ErrorMessage = "Status must be Preparing, Ready, or Collected.")]
    public string Status { get; set; } = string.Empty;
}
