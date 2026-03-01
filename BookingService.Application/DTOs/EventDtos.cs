namespace BookingService.Application.DTOs;

/// <summary>
/// Ticket type with availability information.
/// </summary>
/// <param name="TicketTypeId">Unique identifier of the ticket type.</param>
/// <param name="Name">Name of the ticket type.</param>
/// <param name="Price">Price per ticket.</param>
/// <param name="Capacity">Total capacity.</param>
/// <param name="Reserved">Number of reserved tickets.</param>
/// <param name="Remaining">Number of available tickets.</param>
public record TicketTypeAvailabilityDto(
    Guid TicketTypeId,
    string Name,
    decimal Price,
    int Capacity,
    int Reserved,
    int Remaining
);

/// <summary>
/// Summary of an event for listing purposes.
/// </summary>
/// <param name="Id">Unique identifier of the event.</param>
/// <param name="Title">Event title.</param>
/// <param name="Location">Event location.</param>
/// <param name="StartAt">Event start time.</param>
/// <param name="EndAt">Event end time.</param>
/// <param name="Category">Event category.</param>
/// <param name="TicketTypes">Available ticket types with availability.</param>
public record EventSummaryDto(
    Guid Id,
    string Title,
    string Location,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string Category,
    IReadOnlyList<TicketTypeAvailabilityDto> TicketTypes
);

/// <summary>
/// Detailed event information.
/// </summary>
/// <param name="Id">Unique identifier of the event.</param>
/// <param name="Title">Event title.</param>
/// <param name="Description">Event description.</param>
/// <param name="Category">Event category.</param>
/// <param name="Location">Event location.</param>
/// <param name="OrganizerId">ID of the event organizer.</param>
/// <param name="StartAt">Event start time.</param>
/// <param name="EndAt">Event end time.</param>
/// <param name="Status">Event status (Draft, Published, Cancelled).</param>
/// <param name="TicketTypes">Available ticket types with availability.</param>
public record EventDetailDto(
    Guid Id,
    string Title,
    string Description,
    string Category,
    string Location,
    Guid OrganizerId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string Status,
    IReadOnlyList<TicketTypeAvailabilityDto> TicketTypes
);

/// <summary>
/// Statistics for a ticket type.
/// </summary>
/// <param name="TicketTypeId">Unique identifier of the ticket type.</param>
/// <param name="Name">Name of the ticket type.</param>
/// <param name="Capacity">Total capacity.</param>
/// <param name="Sold">Number of sold tickets.</param>
/// <param name="Reserved">Number of reserved tickets.</param>
/// <param name="Remaining">Number of available tickets.</param>
public record TicketTypeStatsDto(
    Guid TicketTypeId,
    string Name,
    int Capacity,
    int Sold,
    int Reserved,
    int Remaining
);

/// <summary>
/// Event statistics for organizers.
/// </summary>
/// <param name="EventId">Unique identifier of the event.</param>
/// <param name="EventTitle">Event title.</param>
/// <param name="TotalRevenue">Total revenue from confirmed bookings.</param>
/// <param name="OccupancyRate">Percentage of tickets sold.</param>
/// <param name="CancelledCount">Number of cancelled bookings.</param>
/// <param name="TicketsSoldPerType">Breakdown of sales by ticket type.</param>
public record EventStatsDto(
    Guid EventId,
    string EventTitle,
    decimal TotalRevenue,
    double OccupancyRate,
    int CancelledCount,
    IReadOnlyList<TicketTypeStatsDto> TicketsSoldPerType
);

/// <summary>
/// Request to create a ticket type.
/// </summary>
/// <param name="Name">Name of the ticket type.</param>
/// <param name="Price">Price per ticket.</param>
/// <param name="Capacity">Total capacity.</param>
public record TicketTypeCreateDto(string Name, decimal Price, int Capacity);

/// <summary>
/// Request to create a new event.
/// </summary>
/// <param name="Title">Event title.</param>
/// <param name="Description">Event description.</param>
/// <param name="Category">Event category.</param>
/// <param name="Location">Event location.</param>
/// <param name="StartAt">Event start time.</param>
/// <param name="EndAt">Event end time.</param>
/// <param name="TicketTypes">Ticket types to create.</param>
public record CreateEventRequest(
    string Title,
    string Description,
    string Category,
    string Location,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    IEnumerable<TicketTypeCreateDto> TicketTypes
);

/// <summary>
/// Request to update a ticket type.
/// </summary>
/// <param name="Id">ID of the ticket type to update.</param>
/// <param name="Name">Updated name.</param>
/// <param name="Price">Updated price.</param>
/// <param name="Capacity">Updated capacity.</param>
public record TicketTypeUpdateDto(Guid Id, string Name, decimal Price, int Capacity);

/// <summary>
/// Request to update an event.
/// </summary>
/// <param name="Title">Updated title.</param>
/// <param name="Description">Updated description.</param>
/// <param name="Category">Updated category.</param>
/// <param name="Location">Updated location.</param>
/// <param name="StartAt">Updated start time.</param>
/// <param name="EndAt">Updated end time.</param>
/// <param name="TicketTypes">Ticket types to update.</param>
public record UpdateEventRequest(
    string Title,
    string Description,
    string Category,
    string Location,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    IEnumerable<TicketTypeUpdateDto> TicketTypes
);
