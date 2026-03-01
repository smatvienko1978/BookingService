namespace BookingService.Core.Entities;
public sealed record TicketTypeAvailability(
    Guid TicketTypeId,
    int Capacity,
    int Reserved,
    int Remaining);
