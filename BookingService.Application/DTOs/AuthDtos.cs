using BookingService.Core.Enums;

namespace BookingService.Application.DTOs;

/// <summary>
/// Request to register a new user.
/// </summary>
/// <param name="Email">User's email address.</param>
/// <param name="Password">User's password.</param>
/// <param name="FullName">User's full name.</param>
/// <param name="Role">User's role (Customer, Organizer, Admin).</param>
public record RegisterRequest(string Email, string Password, string FullName, UserRole Role);

/// <summary>
/// Request to authenticate a user.
/// </summary>
/// <param name="Email">User's email address.</param>
/// <param name="Password">User's password.</param>
public record LoginRequest(string Email, string Password);

/// <summary>
/// Authentication response containing user details and JWT token.
/// </summary>
/// <param name="UserId">Authenticated user's ID.</param>
/// <param name="Email">User's email address.</param>
/// <param name="FullName">User's full name.</param>
/// <param name="Role">User's role.</param>
/// <param name="Token">JWT authentication token.</param>
public record AuthResponse(Guid UserId, string Email, string FullName, string Role, string Token);
