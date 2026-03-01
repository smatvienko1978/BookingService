using BookingService.Core.Enums;

namespace BookingService.Core.Entities;

/// <summary>
/// Represents a refund issued for a cancelled booking.
/// </summary>
public class Refund
{
    /// <summary>
    /// Unique identifier of the refund.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the booking this refund is for.
    /// </summary>
    public Guid BookingId { get; set; }

    /// <summary>
    /// Refund amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// When the refund was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Reason for the refund.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Current status of the refund (Pending, Completed, Failed).
    /// </summary>
    public RefundStatus Status { get; set; }

    /// <summary>
    /// Navigation property to the associated booking.
    /// </summary>
    public Booking Booking { get; set; } = default!;
}
