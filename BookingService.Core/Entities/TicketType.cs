using BookingService.Core.Exceptions;

namespace BookingService.Core.Entities;

/// <summary>
/// Represents a type of ticket available for an event (e.g., VIP, Regular, Student).
/// 
/// This entity implements the core ticket inventory management logic using a three-state model:
/// - Available: Tickets that can be purchased (Capacity - Reserved - Sold)
/// - Reserved: Tickets held for pending bookings (not yet paid)
/// - Sold: Tickets for confirmed bookings (paid)
/// 
/// The state transitions are:
/// - Reserve(): Available → Reserved (when booking is created)
/// - Confirm(): Reserved → Sold (when booking is confirmed/paid)
/// - Release(): Reserved → Available (when pending booking expires or is cancelled)
/// - ReturnSold(): Sold → Available (when confirmed booking is cancelled with refund)
/// 
/// Concurrency is handled via RowVersion (optimistic locking) to prevent overselling
/// when multiple users try to book the last available tickets simultaneously.
/// </summary>
public class TicketType
{
    /// <summary>
    /// Unique identifier of the ticket type.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// ID of the event this ticket type belongs to.
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Name of the ticket type.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Price per ticket.
    /// </summary>
    public decimal Price { get; private set; }

    /// <summary>
    /// Total number of tickets available for this type.
    /// </summary>
    public int Capacity { get; private set; }

    /// <summary>
    /// Number of tickets currently reserved (pending bookings).
    /// </summary>
    public int ReservedQuantity { get; private set; }

    /// <summary>
    /// Number of tickets sold (confirmed bookings).
    /// </summary>
    public int SoldQuantity { get; private set; }

    /// <summary>
    /// Whether this ticket type is available for purchase.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Navigation property to the parent event.
    /// </summary>
    public Event Event { get; private set; } = default!;

    /// <summary>
    /// Concurrency token for optimistic locking.
    /// </summary>
    public byte[] RowVersion { get; set; } = default!;

    /// <summary>
    /// Number of tickets available for purchase.
    /// </summary>
    public int Available => Capacity - ReservedQuantity - SoldQuantity;

    private TicketType() { }

    /// <summary>
    /// Creates a new ticket type for an event.
    /// </summary>
    /// <param name="eventId">ID of the parent event.</param>
    /// <param name="name">Name of the ticket type.</param>
    /// <param name="price">Price per ticket.</param>
    /// <param name="capacity">Total capacity.</param>
    /// <returns>A new ticket type instance.</returns>
    public static TicketType Create(Guid eventId, string name, decimal price, int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity, nameof(capacity));
        return new TicketType
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Name = name,
            Price = price,
            Capacity = capacity,
            ReservedQuantity = 0,
            SoldQuantity = 0,
            IsActive = true,
            RowVersion = []
        };
    }

    /// <summary>
    /// Reserves tickets for a pending booking.
    /// </summary>
    /// <param name="quantity">Number of tickets to reserve.</param>
    public void Reserve(int quantity)
    {
        if (!IsActive)
            throw new ValidationException("Inactive ticket type.");

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));

        if (Available < quantity)
            throw new CapacityExceededException("Not enough tickets.");

        ReservedQuantity += quantity;
    }

    /// <summary>
    /// Confirms reserved tickets, moving them from reserved to sold.
    /// </summary>
    /// <param name="quantity">Number of tickets to confirm.</param>
    public void Confirm(int quantity)
    {
        if (ReservedQuantity < quantity)
            throw new InvalidOperationException();

        ReservedQuantity -= quantity;
        SoldQuantity += quantity;
    }

    /// <summary>
    /// Releases reserved tickets back to available (for expired/cancelled pending bookings).
    /// </summary>
    /// <param name="quantity">Number of tickets to release.</param>
    public void Release(int quantity)
    {
        if (ReservedQuantity < quantity)
            throw new InvalidOperationException();

        ReservedQuantity -= quantity;
    }

    /// <summary>
    /// Returns sold tickets back to available (for cancelled confirmed bookings).
    /// </summary>
    /// <param name="quantity">Number of tickets to return.</param>
    public void ReturnSold(int quantity)
    {
        if (SoldQuantity < quantity)
            throw new InvalidOperationException();

        SoldQuantity -= quantity;
    }

    /// <summary>
    /// Changes the total capacity of this ticket type.
    /// </summary>
    /// <param name="newCapacity">New capacity value.</param>
    public void ChangeCapacity(int newCapacity)
    {
        if (newCapacity < SoldQuantity + ReservedQuantity)
            throw new InvalidOperationException("Cannot reduce below sold/reserved.");

        Capacity = newCapacity;
    }

    /// <summary>
    /// Updates the name and price of this ticket type.
    /// </summary>
    /// <param name="name">New name.</param>
    /// <param name="price">New price.</param>
    public void UpdateDetails(string name, decimal price)
    {
        Name = name;
        Price = price;
    }
}
