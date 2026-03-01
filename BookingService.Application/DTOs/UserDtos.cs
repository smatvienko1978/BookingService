using BookingService.Core.Enums;

namespace BookingService.Application.DTOs;

/// <summary>
/// User information for display purposes.
/// </summary>
/// <param name="Id">Unique identifier of the user.</param>
/// <param name="Email">User's email address.</param>
/// <param name="FullName">User's full name.</param>
/// <param name="Role">User's role.</param>
/// <param name="CreatedAt">When the user was created.</param>
public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    DateTimeOffset CreatedAt
);

/// <summary>
/// Request to create a new user (admin operation).
/// </summary>
/// <param name="Email">User's email address.</param>
/// <param name="Password">User's password.</param>
/// <param name="FullName">User's full name.</param>
/// <param name="Role">User's role.</param>
public record CreateUserRequest(
    string Email,
    string Password,
    string FullName,
    UserRole Role
);

/// <summary>
/// Request to update a user's information.
/// </summary>
/// <param name="Email">Updated email address.</param>
/// <param name="FullName">Updated full name.</param>
public record UpdateUserRequest(
    string Email,
    string FullName
);
