namespace BookingService.Core.Exceptions;

/// <summary>
/// Thrown when a booking operation is not allowed due to current state
/// (e.g. confirm when already cancelled, cancel when expired).
/// Maps to HTTP 400 Bad Request.
/// </summary>
public class InvalidBookingStateException : ValidationException
{
    public InvalidBookingStateException(string message, Exception? innerException = null)
        : base(message, "InvalidBookingState", innerException) { }
}
