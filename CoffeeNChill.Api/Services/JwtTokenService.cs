
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using CoffeeNChill.Api.Models;

namespace CoffeeNChill.Api.Services;

public class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAtUtc) CreateToken(
        UserEntity user)
    {
        string signingKey =
            _configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException(
                "Jwt:SigningKey is not configured.");

        if (Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must be at least 32 bytes.");
        }

        string issuer = _configuration["Jwt:Issuer"]
            ?? "CoffeeNChill.Api";

        string audience = _configuration["Jwt:Audience"]
            ?? "CoffeeNChill.Client";

        int minutes = int.TryParse(
            _configuration["Jwt:ExpirationMinutes"], out int configured)
            ? Math.Clamp(configured, 5, 120)
            : 60;

        DateTime expiresAt = DateTime.UtcNow.AddMinutes(minutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.RowKey),
            new Claim(ClaimTypes.Role, user.PartitionKey),
            new Claim(JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(signingKey));

        var credentials = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return (
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }
}
