using BookingService.Application.DTOs;
using BookingService.Core.Request;

namespace BookingService.Application.Interfaces;

/// <summary>
/// Service for managing ticket bookings.
/// </summary>
public interface IBookingsService
{
    /// <summary>
    /// Creates a new pending booking for the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user making the booking.</param>
    /// <param name="request">The booking request containing event and ticket details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created booking details.</returns>
    Task<BookingDto> Create(Guid userId, CreateBookingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a pending booking, converting reserved tickets to sold.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to confirm.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Confirm(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a booking and processes refund if applicable.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to cancel.</param>
    /// <param name="userId">The ID of the user requesting cancellation.</param>
    /// <param name="reason">Optional cancellation reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated booking details with refund information if applicable.</returns>
    Task<BookingDto> Cancel(Guid bookingId, Guid userId, string? reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a booking by its ID.
    /// </summary>
    /// <param name="bookingId">The ID of the booking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The booking details, or null if not found.</returns>
    Task<BookingDto?> GetById(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all bookings for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of the user's bookings.</returns>
    Task<IEnumerable<BookingDto>> GetByUser(Guid userId, CancellationToken cancellationToken = default);
}
