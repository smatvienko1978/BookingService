using BookingService.Core.Entities;

namespace BookingService.Application.Interfaces;

/// <summary>
/// Service for evaluating booking cancellation policies.
/// </summary>
public interface IBookingPolicyService
{
    /// <summary>
    /// Evaluates whether a booking can be cancelled and calculates the refund amount.
    /// </summary>
    /// <param name="booking">The booking to evaluate.</param>
    /// <param name="evt">The event associated with the booking.</param>
    /// <returns>Result indicating if cancellation is allowed and the refund amount.</returns>
    /// <remarks>
    /// Policy rules:
    /// - Pending bookings: always allowed, no refund (no payment taken).
    /// - Confirmed bookings: allowed only if current time is before the refund cutoff; full refund in that case.
    /// - Within refund cutoff of event start: cancellation disallowed.
    /// - Refund cutoff is configurable via BookingOptions.RefundCutoffHours.
    /// </remarks>
    CancellationResult EvaluateCancellation(Booking booking, Event evt);
}

/// <summary>
/// Result of a cancellation policy evaluation.
/// </summary>
/// <param name="Allowed">Whether the cancellation is allowed.</param>
/// <param name="RefundAmount">The refund amount if cancellation is allowed.</param>
/// <param name="DenialReason">The reason for denial if cancellation is not allowed.</param>
public record CancellationResult(bool Allowed, decimal RefundAmount, string? DenialReason);
