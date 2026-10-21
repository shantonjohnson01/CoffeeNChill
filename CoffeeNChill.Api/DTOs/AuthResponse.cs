
namespace CoffeeNChill.Api.DTOs;

public record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    UserResponse User);
