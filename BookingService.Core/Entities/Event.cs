using BookingService.Core.Enums;

namespace BookingService.Core.Entities;

/// <summary>
/// Represents an event that users can book tickets for.
/// </summary>
public class Event
{
    /// <summary>
    /// Unique identifier of the event.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the user who organizes this event.
    /// </summary>
    public Guid OrganizerId { get; set; }

    /// <summary>
    /// Title of the event.
    /// </summary>
    public string Title { get; set; } = default!;

    /// <summary>
    /// Detailed description of the event.
    /// </summary>
    public string Description { get; set; } = default!;

    /// <summary>
    /// Category of the event (e.g., Concert, Conference, Sports).
    /// </summary>
    public string Category { get; set; } = default!;

    /// <summary>
    /// Physical location where the event takes place.
    /// </summary>
    public string Location { get; set; } = default!;

    /// <summary>
    /// When the event starts.
    /// </summary>
    public DateTimeOffset StartAt { get; set; }

    /// <summary>
    /// When the event ends.
    /// </summary>
    public DateTimeOffset EndAt { get; set; }

    /// <summary>
    /// Current status of the event (Draft, Published, Cancelled).
    /// </summary>
    public EventStatus Status { get; set; }

    /// <summary>
    /// When the event was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the event was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to the event organizer.
    /// </summary>
    public User Organizer { get; set; } = default!;

    /// <summary>
    /// Collection of ticket types available for this event.
    /// </summary>
    public ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();

    /// <summary>
    /// Collection of bookings made for this event.
    /// </summary>
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
