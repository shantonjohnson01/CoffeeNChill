
using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Api.DTOs;

public class CreateUserRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 12)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Staff|Admin)$",
        ErrorMessage = "Role must be Staff or Admin.")]
    public string Role { get; set; } = "Staff";
}
