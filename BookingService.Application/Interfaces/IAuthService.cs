using BookingService.Application.DTOs;

namespace BookingService.Application.Interfaces;

/// <summary>
/// Service for user authentication and registration.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user and returns authentication credentials.
    /// </summary>
    /// <param name="request">The registration request containing user details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication response with JWT token, or null if email already exists.</returns>
    Task<AuthResponse?> Register(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="request">The login request containing credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication response with JWT token, or null if credentials are invalid.</returns>
    Task<AuthResponse?> Login(LoginRequest request, CancellationToken cancellationToken = default);
}
