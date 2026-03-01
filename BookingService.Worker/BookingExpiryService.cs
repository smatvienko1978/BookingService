using BookingService.Application.Interfaces;
using BookingService.Core.Entities;
using BookingService.Core.Enums;
using BookingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookingService.Worker;

public class BookingExpiryService(
    IServiceScopeFactory scopeFactory,
    ILogger<BookingExpiryService> logger,
    IOptions<BookingOptions> options,
    ITimeProvider timeProvider) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly ILogger<BookingExpiryService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly BookingOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ITimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Booking expiry service starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredBookingsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing expired bookings.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_options.ExpiryPollIntervalMinutes), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Ignore cancellation during delay.
            }
        }

        _logger.LogInformation("Booking expiry service stopping.");
    }

    private async Task ProcessExpiredBookingsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();

        var now = _timeProvider.UtcNow;

        var expiredPendingBookings = await dbContext.Bookings
            .Where(b => b.Status == BookingStatus.Pending && b.ExpiresAt <= now)
            .Include(b => b.Items)
            .Include(b => b.Event)
                .ThenInclude(e => e.TicketTypes)
            .ToListAsync(cancellationToken);

        if (expiredPendingBookings.Count == 0)
        {
            return;
        }

        foreach (var booking in expiredPendingBookings)
        {
            booking.Expire(now);
            cancellationToken.ThrowIfCancellationRequested();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Marked {Count} pending bookings as expired.", expiredPendingBookings.Count);
    }
}

