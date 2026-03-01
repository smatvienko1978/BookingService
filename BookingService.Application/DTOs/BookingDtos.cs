namespace BookingService.Application.DTOs;

/// <summary>
/// Request to cancel a booking.
/// </summary>
/// <param name="Reason">Optional reason for cancellation.</param>
public record CancelBookingRequest(string? Reason);

/// <summary>
/// Represents a single item in a booking.
/// </summary>
/// <param name="Id">Unique identifier of the booking item.</param>
/// <param name="TicketTypeId">ID of the ticket type.</param>
/// <param name="TicketTypeName">Name of the ticket type.</param>
/// <param name="Quantity">Number of tickets.</param>
/// <param name="UnitPrice">Price per ticket.</param>
/// <param name="TotalAmount">Total amount for this item.</param>
public record BookingItemDto(
    Guid Id,
    Guid TicketTypeId,
    string TicketTypeName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount
);

/// <summary>
/// Represents a booking with all its details.
/// </summary>
/// <param name="Id">Unique identifier of the booking.</param>
/// <param name="EventId">ID of the booked event.</param>
/// <param name="EventTitle">Title of the booked event.</param>
/// <param name="Status">Current status (Pending, Confirmed, Cancelled, Expired).</param>
/// <param name="CreatedAt">When the booking was created.</param>
/// <param name="ExpiresAt">When the pending booking expires.</param>
/// <param name="ConfirmedAt">When the booking was confirmed, if applicable.</param>
/// <param name="CancelledAt">When the booking was cancelled, if applicable.</param>
/// <param name="CancellationReason">Reason for cancellation, if applicable.</param>
/// <param name="TotalAmount">Total amount for the booking.</param>
/// <param name="Items">List of booking items.</param>
/// <param name="Refund">Refund details, if applicable.</param>
public record BookingDto(
    Guid Id,
    Guid EventId,
    string EventTitle,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    decimal TotalAmount,
    IReadOnlyList<BookingItemDto> Items,
    RefundDto? Refund
);

/// <summary>
/// Represents a refund for a cancelled booking.
/// </summary>
/// <param name="Id">Unique identifier of the refund.</param>
/// <param name="Amount">Refund amount.</param>
/// <param name="Status">Current status (Pending, Completed, Failed).</param>
/// <param name="CreatedAt">When the refund was created.</param>
/// <param name="Reason">Reason for the refund.</param>
public record RefundDto(
    Guid Id,
    decimal Amount,
    string Status,
    DateTimeOffset CreatedAt,
    string? Reason
);
