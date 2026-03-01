using BookingService.Application.DTOs;

namespace BookingService.Application.Interfaces;

/// <summary>
/// Service for public event browsing.
/// </summary>
public interface IEventsService
{
    /// <summary>
    /// Retrieves published events available for booking with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated collection of published events with availability information.</returns>
    Task<PaginatedResponse<EventSummaryDto>> GetPublished(PaginationRequest pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific published event by its ID.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The event details with ticket availability, or null if not found or not published.</returns>
    Task<EventDetailDto?> GetById(Guid eventId, CancellationToken cancellationToken = default);
}
