using BookingService.Application.DTOs;

namespace BookingService.Application.Interfaces;

/// <summary>
/// Service for event organizers to manage their events.
/// </summary>
public interface IOrganizerEventsService
{
    /// <summary>
    /// Retrieves all events owned by a specific organizer.
    /// </summary>
    /// <param name="organizerId">The ID of the organizer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of the organizer's events.</returns>
    Task<IEnumerable<EventDetailDto>> GetByOrganizer(Guid organizerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific event owned by the organizer.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="organizerId">The ID of the organizer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The event details, or null if not found or not owned by organizer.</returns>
    Task<EventDetailDto?> GetById(Guid eventId, Guid organizerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves statistics for a specific event.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="organizerId">The ID of the organizer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Event statistics including revenue and ticket sales, or null if not found.</returns>
    Task<EventStatsDto?> GetStats(Guid eventId, Guid organizerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new event for the organizer.
    /// </summary>
    /// <param name="organizerId">The ID of the organizer.</param>
    /// <param name="request">The event creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created event details.</returns>
    Task<EventDetailDto> Create(Guid organizerId, CreateEventRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    /// <param name="eventId">The ID of the event to update.</param>
    /// <param name="organizerId">The ID of the organizer.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the event was updated, false if not found or not owned by organizer.</returns>
    Task<bool> Update(Guid eventId, Guid organizerId, UpdateEventRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an event.
    /// </summary>
    /// <param name="eventId">The ID of the event to delete.</param>
    /// <param name="organizerId">The ID of the organizer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the event was deleted, false if not found or not owned by organizer.</returns>
    Task<bool> Delete(Guid eventId, Guid organizerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a draft event, making it visible to customers for booking.
    /// </summary>
    /// <param name="eventId">The ID of the event to publish.</param>
    /// <param name="organizerId">The ID of the organizer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the event was published, false if not found, not owned, or not in Draft status.</returns>
    Task<bool> Publish(Guid eventId, Guid organizerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a published event.
    /// </summary>
    /// <param name="eventId">The ID of the event to cancel.</param>
    /// <param name="organizerId">The ID of the organizer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the event was cancelled, false if not found or not owned by organizer.</returns>
    Task<bool> Cancel(Guid eventId, Guid organizerId, CancellationToken cancellationToken = default);
}
