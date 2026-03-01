using BookingService.Application.Interfaces;

namespace BookingService.Application.Services;

/// <summary>
/// Production implementation of <see cref="ITimeProvider"/> that returns the actual system time.
/// </summary>
public class SystemTimeProvider : ITimeProvider
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
