namespace BookingService.Core.Exceptions;

/// <summary>
/// Thrown when requested ticket quantity exceeds available capacity.
/// Maps to HTTP 409 Conflict.
/// </summary>
public class CapacityExceededException : ConflictException
{
    public CapacityExceededException(string message = "Not enough tickets available.", Exception? innerException = null)
        : base(message, "CapacityExceeded", innerException) { }
}
