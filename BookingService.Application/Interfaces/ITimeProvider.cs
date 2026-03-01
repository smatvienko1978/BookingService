namespace BookingService.Application.Interfaces;

/// <summary>
/// Provides an abstraction for accessing the current time.
/// Enables testability by allowing time to be controlled in unit tests.
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
