namespace BookingService.Core.Exceptions;

/// <summary>
/// Thrown when a business rule or validation fails.
/// Maps to HTTP 400 Bad Request.
/// </summary>
public class ValidationException : DomainException
{
    public ValidationException(string message, string? errorCode = "ValidationError", Exception? innerException = null)
        : base(message, errorCode, innerException) { }
}
