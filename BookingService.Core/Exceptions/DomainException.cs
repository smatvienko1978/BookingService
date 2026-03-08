namespace BookingService.Core.Exceptions;

/// <summary>
/// Base exception for domain and application-level errors.
/// Enables consistent handling and mapping to HTTP status codes (e.g. ProblemDetails).
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Optional machine-readable error code (e.g. "InvalidBookingState", "CapacityExceeded").
    /// </summary>
    public string? ErrorCode { get; }

    protected DomainException(string message, string? errorCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
