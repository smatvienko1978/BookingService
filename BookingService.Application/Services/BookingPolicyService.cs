using BookingService.Application.Interfaces;
using BookingService.Core.Entities;
using BookingService.Core.Enums;
using Microsoft.Extensions.Options;

namespace BookingService.Application.Services;

/// <summary>
/// Implementation of booking cancellation policy evaluation.
/// </summary>
public class BookingPolicyService(
    IOptions<BookingOptions> options,
    ITimeProvider timeProvider) : IBookingPolicyService
{
    private readonly BookingOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ITimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <inheritdoc />
    public CancellationResult EvaluateCancellation(Booking booking, Event evt)
    {
        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Expired)
            return new CancellationResult(false, 0, "Booking is already cancelled or expired.");

        if (booking.Status == BookingStatus.Pending)
            return new CancellationResult(true, 0, null);

        if (booking.Status != BookingStatus.Confirmed)
            return new CancellationResult(false, 0, "Only pending or confirmed bookings can be cancelled.");

        var refundCutoff = TimeSpan.FromHours(_options.RefundCutoffHours);
        var cutoff = evt.StartAt - refundCutoff;

        if (_timeProvider.UtcNow >= cutoff)
            return new CancellationResult(false, 0, $"Cancellation is not allowed within {_options.RefundCutoffHours} hours of the event start. No refund available.");

        return new CancellationResult(true, booking.TotalAmount, null);
    }
}
