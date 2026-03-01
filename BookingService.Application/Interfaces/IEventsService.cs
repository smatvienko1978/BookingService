using BookingService.Application.DTOs;

namespace BookingService.Application.Interfaces;

/// <summary>
/// Service for public event browsing.
/// </summary>
public interface IEventsService
{
    /// <summary>
    /// Retrieves all published events available for booking.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of published events with availability information.</returns>
    Task<IEnumerable<EventSummaryDto>> GetPublished(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific published event by its ID.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The event details with ticket availability, or null if not found or not published.</returns>
    Task<EventDetailDto?> GetById(Guid eventId, CancellationToken cancellationToken = default);
}
