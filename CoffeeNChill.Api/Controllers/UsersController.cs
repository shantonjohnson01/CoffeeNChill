using Azure;
using BC = BCrypt.Net.BCrypt;
using CoffeeNChill.Api.DTOs;
using CoffeeNChill.Api.Models;
using CoffeeNChill.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Api.Controllers;

/// <summary>Allows Admins to create staff and administrator accounts.</summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserTableService _users;

    public UsersController(UserTableService users)
    {
        _users = users;
    }

    /// <summary>Creates a Staff or Admin account.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request)
    {
        string email = UserTableService.NormalizeEmail(request.Email);

        if (await _users.FindByEmailAsync(email) is not null)
        {
            return Conflict(new { error = "An account with this email already exists." });
        }

        var user = new UserEntity
        {
            PartitionKey = request.Role,
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

        return Created(
            "/api/users",
            new UserResponse(
                user.FullName,
                user.RowKey,
                user.PartitionKey,
                user.CreatedDate));
    }
}