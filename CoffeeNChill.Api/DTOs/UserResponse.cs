
namespace CoffeeNChill.Api.DTOs;

public record UserResponse(
    string FullName,
    string Email,
    string Role,
    DateTime CreatedDate);
