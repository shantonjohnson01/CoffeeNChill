using System.ComponentModel.DataAnnotations;
using Azure;
using BC = BCrypt.Net.BCrypt;
using CoffeeNChill.Api.DTOs;
using CoffeeNChill.Api.Models;
using CoffeeNChill.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Api.Controllers;

/// <summary>Registers Customer accounts and authenticates users.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserTableService _users;
    private readonly JwtTokenService _tokens;

    public AuthController(
        UserTableService users,
        JwtTokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    /// <summary>Registers a new Customer account.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request)
    {
        string email = UserTableService.NormalizeEmail(request.Email);

        if (await _users.FindByEmailAsync(email) is not null)
        {
            return Conflict(new { error = "An account with this email already exists." });
        }

        var user = new UserEntity
        {
            PartitionKey = "Customer",
            RowKey = email,
            FullName = request.FullName.Trim(),
            PasswordHash = BC.HashPassword(request.Password),
            CreatedDate = DateTime.UtcNow
        };

        try
        {
            await _users.CreateAsync(user);
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            return Conflict(new { error = "An account with this email already exists." });
        }

        var response = new UserResponse(
            user.FullName, user.RowKey, user.PartitionKey, user.CreatedDate);

        return Created("/api/auth/login", response);
    }

    /// <summary>Validates credentials and returns a JWT Bearer token.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        var user = await _users.FindByEmailAsync(request.Email);

        if (user is null ||
            !BC.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new
            {
                error = "Invalid email or password."
            });
        }

        var (token, expiresAt) = _tokens.CreateToken(user);

        return Ok(new AuthResponse(
            token,
            expiresAt,
            new UserResponse(
                user.FullName,
                user.RowKey,
                user.PartitionKey,
                user.CreatedDate)));
    }
}