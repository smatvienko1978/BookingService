using BookingService.Core.Enums;

namespace BookingService.Core.Entities;

public class Booking
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid EventId { get; private set; }
    public BookingStatus Status { get; private set; } = BookingStatus.Pending;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public decimal TotalAmount { get; private set; }
    public User User { get; set; } = default!;
    public Event Event { get; set; } = default!;
    public Refund? Refund { get; set; }
    public byte[] RowVersion { get; set; } = default!;

    public IReadOnlyCollection<BookingItem> Items => _items.AsReadOnly();
    private readonly List<BookingItem> _items = [];

    private Booking() { }

    /// <summary>
    /// Constructor for creating a new pending booking.
    /// </summary>
    public Booking(Guid userId, Guid eventId, DateTimeOffset expiresAt, User user, Event evt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        EventId = eventId;
        Status = BookingStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
        ConfirmedAt = null;
        CancelledAt = null;
        CancellationReason = null;
        TotalAmount = 0;
        User = user;
        Event = evt;
        Refund = null;
        RowVersion = [];
    }

    /// <summary>
    /// Constructor for test purposes only.
    /// </summary>
    internal Booking(Guid userId, Guid eventId, BookingStatus status, DateTimeOffset createdAt, DateTimeOffset expiresAt, DateTimeOffset? confirmedAt, DateTimeOffset? cancelledAt, string? cancellationReason, decimal totalAmount, User user, Event @event, Refund? refund, byte[] rowVersion)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        EventId = eventId;
        Status = status;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        ConfirmedAt = confirmedAt;
        CancelledAt = cancelledAt;
        CancellationReason = cancellationReason;
        TotalAmount = totalAmount;
        User = user;
        Event = @event;
        Refund = refund;
        RowVersion = rowVersion;
    }


    public void AddItem(TicketType ticketType, int quantity)
    {
        if (Status != BookingStatus.Pending)
            throw new InvalidOperationException("Cannot modify confirmed or cancelled booking.");

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));

        ticketType.Reserve(quantity);

        var item = new BookingItem(this, ticketType, quantity);
        _items.Add(item);

        TotalAmount = _items.Sum(i => i.TotalAmount);
    }

    public void Confirm(DateTimeOffset now)
    {
        if (Status != BookingStatus.Pending)
            throw new InvalidOperationException();
        
        if (ExpiresAt < now)
            throw new InvalidOperationException();
        
        foreach (var item in _items)
        {
            var ticketType = Event.TicketTypes.First(t => t.Id == item.TicketTypeId);
            ticketType.Confirm(item.Quantity);
        }

        Status = BookingStatus.Confirmed;
        ConfirmedAt = now;
    }

    public void Cancel(DateTimeOffset now, string reason)
    {
        if (Status is BookingStatus.Cancelled or BookingStatus.Expired)
            throw new InvalidOperationException();

        foreach (var item in _items)
        {
            var ticketType = Event.TicketTypes.First(t => t.Id == item.TicketTypeId);
            if (Status == BookingStatus.Pending)
                ticketType.Release(item.Quantity);
            else if (Status == BookingStatus.Confirmed)
                ticketType.ReturnSold(item.Quantity);
        }

        Status = BookingStatus.Cancelled;
        CancelledAt = now;
        CancellationReason = reason;
    }

    public void Expire(DateTimeOffset now)
    {
        if (Status != BookingStatus.Pending)
            throw new InvalidOperationException();
        
        if (ExpiresAt > now)
            throw new InvalidOperationException();
        
        foreach (var item in _items)
        {
            var ticketType = Event.TicketTypes.First(t => t.Id == item.TicketTypeId);
            ticketType.Release(item.Quantity);
        }

        Status = BookingStatus.Expired;
    }
}

