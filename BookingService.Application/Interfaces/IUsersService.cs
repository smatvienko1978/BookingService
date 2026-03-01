using BookingService.Application.DTOs;

namespace BookingService.Application.Interfaces;

/// <summary>
/// Service for managing user accounts (admin operations).
/// </summary>
public interface IUsersService
{
    /// <summary>
    /// Retrieves all users in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of all users.</returns>
    Task<IEnumerable<UserDto>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by their ID.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user details, or null if not found.</returns>
    Task<UserDto?> GetById(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    /// <param name="request">The user creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user details.</returns>
    Task<UserDto> Create(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user's information.
    /// </summary>
    /// <param name="userId">The ID of the user to update.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user was updated, false if not found.</returns>
    Task<bool> Update(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user account.
    /// </summary>
    /// <param name="userId">The ID of the user to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user was deleted, false if not found.</returns>
    Task<bool> Delete(Guid userId, CancellationToken cancellationToken = default);
}
