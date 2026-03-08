namespace BookingService.Core.Exceptions;

/// <summary>
/// Thrown when an operation conflicts with current state (e.g. concurrency, duplicate, capacity).
/// Maps to HTTP 409 Conflict.
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message, string? errorCode = "Conflict", Exception? innerException = null)
        : base(message, errorCode, innerException) { }
}
