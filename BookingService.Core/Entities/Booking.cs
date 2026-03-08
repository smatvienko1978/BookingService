using BookingService.Core.Enums;
using BookingService.Core.Exceptions;

namespace BookingService.Core.Entities;

/// <summary>
/// Represents a ticket booking made by a user for an event.
/// 
/// Booking Lifecycle:
/// 1. PENDING: Created when user selects tickets. Tickets are reserved but not sold.
///    Has an expiration time (configurable, default 15 min) for payment.
/// 2. CONFIRMED: User completed payment. Reserved tickets become sold.
/// 3. CANCELLED: User or system cancelled. Tickets returned to available pool.
///    If confirmed booking cancelled >24h before event, full refund is issued.
/// 4. EXPIRED: Pending booking timed out. Reserved tickets auto-released.
/// 
/// State Transitions:
/// - Pending → Confirmed (via Confirm())
/// - Pending → Cancelled (via Cancel())
/// - Pending → Expired (via Expire() - called by background worker)
/// - Confirmed → Cancelled (via Cancel() - subject to refund policy)
/// 
/// A booking can contain multiple items (e.g., 2 VIP + 3 Regular tickets).
/// Total amount is calculated as sum of all items' amounts.
/// </summary>
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


    /// <summary>
    /// Adds a ticket item to this booking and reserves the tickets.
    /// Can only be called on pending bookings.
    /// Automatically recalculates the total amount.
    /// </summary>
    /// <param name="ticketType">The ticket type to book.</param>
    /// <param name="quantity">Number of tickets to book.</param>
    /// <exception cref="InvalidOperationException">If booking is not pending or tickets unavailable.</exception>
    public void AddItem(TicketType ticketType, int quantity)
    {
        if (Status != BookingStatus.Pending)
            throw new InvalidBookingStateException("Cannot modify confirmed or cancelled booking.");

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity, nameof(quantity));

        // Reserve tickets first - this will throw if not enough available
        ticketType.Reserve(quantity);

        // Create booking item with denormalized price (captures price at time of booking)
        var item = new BookingItem(this, ticketType, quantity);
        _items.Add(item);

        // Recalculate total from all items
        TotalAmount = _items.Sum(i => i.TotalAmount);
    }

    /// <summary>
    /// Confirms the booking, converting reserved tickets to sold.
    /// This represents successful payment completion.
    /// </summary>
    /// <param name="now">Current timestamp for audit trail.</param>
    /// <exception cref="InvalidOperationException">If booking is not pending or has expired.</exception>
    public void Confirm(DateTimeOffset now)
    {
        if (Status != BookingStatus.Pending)
            throw new InvalidBookingStateException("Booking cannot be confirmed in its current state.");
        
        // Cannot confirm an expired booking
        if (ExpiresAt < now)
            throw new InvalidBookingStateException("Booking has expired.");
        
        // Move all reserved tickets to sold status
        foreach (var item in _items)
        {
            var ticketType = Event.TicketTypes.First(t => t.Id == item.TicketTypeId);
            ticketType.Confirm(item.Quantity);
        }

        Status = BookingStatus.Confirmed;
        ConfirmedAt = now;
    }

    /// <summary>
    /// Cancels the booking and returns tickets to available pool.
    /// For pending bookings: releases reserved tickets.
    /// For confirmed bookings: returns sold tickets (refund handled separately).
    /// </summary>
    /// <param name="now">Current timestamp for audit trail.</param>
    /// <param name="reason">Reason for cancellation (for audit/support).</param>
    /// <exception cref="InvalidOperationException">If booking is already cancelled or expired.</exception>
    public void Cancel(DateTimeOffset now, string reason)
    {
        if (Status is BookingStatus.Cancelled or BookingStatus.Expired)
            throw new InvalidBookingStateException("Booking is already cancelled or expired.");

        // Return tickets based on current status
        foreach (var item in _items)
        {
            var ticketType = Event.TicketTypes.First(t => t.Id == item.TicketTypeId);
            if (Status == BookingStatus.Pending)
                ticketType.Release(item.Quantity);  // Reserved → Available
            else if (Status == BookingStatus.Confirmed)
                ticketType.ReturnSold(item.Quantity);  // Sold → Available
        }

        Status = BookingStatus.Cancelled;
        CancelledAt = now;
        CancellationReason = reason;
    }

    /// <summary>
    /// Expires a pending booking that has passed its timeout.
    /// Called by the background worker service.
    /// Releases all reserved tickets back to available pool.
    /// </summary>
    /// <param name="now">Current timestamp (must be after ExpiresAt).</param>
    /// <exception cref="InvalidOperationException">If booking is not pending or not yet expired.</exception>
    public void Expire(DateTimeOffset now)
    {
        if (Status != BookingStatus.Pending)
            throw new InvalidBookingStateException("Only pending bookings can expire.");
        
        // Safety check: don't expire bookings that haven't actually timed out
        if (ExpiresAt > now)
            throw new InvalidBookingStateException("Booking has not yet reached expiration time.");
        
        // Release all reserved tickets back to available
        foreach (var item in _items)
        {
            var ticketType = Event.TicketTypes.First(t => t.Id == item.TicketTypeId);
            ticketType.Release(item.Quantity);
        }

        Status = BookingStatus.Expired;
    }
}

