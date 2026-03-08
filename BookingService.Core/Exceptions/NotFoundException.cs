namespace BookingService.Core.Exceptions;

/// <summary>
/// Thrown when a requested entity does not exist.
/// Maps to HTTP 404 Not Found.
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string message, string? errorCode = "NotFound", Exception? innerException = null)
        : base(message, errorCode, innerException) { }
}
