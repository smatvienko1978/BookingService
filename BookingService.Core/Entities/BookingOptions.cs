namespace BookingService.Core.Entities;

/// <summary>
/// Configuration options for booking behavior and policies.
/// </summary>
public class BookingOptions
{
    /// <summary>
    /// Number of minutes before a pending booking expires if not confirmed.
    /// Default is 15 minutes.
    /// </summary>
    public int TimeoutMinutes { get; set; } = 15;

    /// <summary>
    /// Number of hours before an event starts when cancellation with refund is no longer allowed.
    /// Default is 24 hours.
    /// </summary>
    public int RefundCutoffHours { get; set; } = 24;

    /// <summary>
    /// Interval in minutes between checks for expired pending bookings.
    /// Default is 1 minute.
    /// </summary>
    public int ExpiryPollIntervalMinutes { get; set; } = 1;
}
