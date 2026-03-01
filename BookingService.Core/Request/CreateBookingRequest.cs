namespace BookingService.Core.Request;

public record CreateBookingRequest(
    Guid EventId,
    IReadOnlyList<BookingItemRequest> Items
);

public record BookingItemRequest(
    Guid TicketTypeId,
    int Quantity
);
