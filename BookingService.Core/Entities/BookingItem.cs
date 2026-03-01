namespace BookingService.Core.Entities;

/// <summary>
/// Represents a line item in a booking (tickets of a specific type).
/// </summary>
public class BookingItem
{
    /// <summary>
    /// Unique identifier of the booking item.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// ID of the parent booking.
    /// </summary>
    public Guid BookingId { get; private set; }

    /// <summary>
    /// ID of the ticket type.
    /// </summary>
    public Guid TicketTypeId { get; private set; }

    /// <summary>
    /// Name of the ticket type (denormalized for historical reference).
    /// </summary>
    public string TicketTypeName { get; private set; } = default!;

    /// <summary>
    /// Price per ticket at time of booking (denormalized for historical reference).
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// Number of tickets in this item.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Total amount for this item (UnitPrice * Quantity).
    /// </summary>
    public decimal TotalAmount => UnitPrice * Quantity;

    /// <summary>
    /// Navigation property to the parent booking.
    /// </summary>
    public Booking Booking { get; private set; } = default!;

    /// <summary>
    /// Creates a new booking item.
    /// </summary>
    /// <param name="booking">Parent booking.</param>
    /// <param name="ticketType">Ticket type being booked.</param>
    /// <param name="quantity">Number of tickets.</param>
    internal BookingItem(Booking booking, TicketType ticketType, int quantity)
    {
        Booking = booking;
        BookingId = booking.Id;
        TicketTypeId = ticketType.Id;
        TicketTypeName = ticketType.Name;
        UnitPrice = ticketType.Price;
        Quantity = quantity;
    }

    private BookingItem() { }
}
