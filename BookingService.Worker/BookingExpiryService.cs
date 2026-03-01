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

/// <summary>
/// Background service that automatically expires pending bookings that have timed out.
/// 
/// This is a critical component for the booking system's integrity:
/// - Prevents tickets from being held indefinitely by abandoned bookings
/// - Releases reserved tickets back to the available pool
/// - Runs as a hosted service inside the API process
/// 
/// Configuration (via BookingOptions):
/// - TimeoutMinutes: How long pending bookings are valid (default: 15 min)
/// - ExpiryPollIntervalMinutes: How often to check for expired bookings (default: 1 min)
/// 
/// Design considerations:
/// - Uses IServiceScopeFactory to create scoped DbContext (required for background services)
/// - Processes all expired bookings in a single batch for efficiency
/// - Gracefully handles errors without crashing the service
/// - Supports graceful shutdown via CancellationToken
/// 
/// For high-scale scenarios, consider:
/// - Moving to a separate worker process
/// - Using message queue for event-driven expiration
/// - Implementing distributed locking for multi-instance deployments
/// </summary>
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

    /// <summary>
    /// Main execution loop - runs continuously while the application is running.
    /// Polls for expired bookings at configured intervals.
    /// </summary>
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
                // Log and continue - don't let one failure stop the service
                _logger.LogError(ex, "Error while processing expired bookings.");
            }

            try
            {
                // Wait before next poll (configurable interval)
                await Task.Delay(TimeSpan.FromMinutes(_options.ExpiryPollIntervalMinutes), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected during shutdown - ignore and exit loop
            }
        }

        _logger.LogInformation("Booking expiry service stopping.");
    }

    /// <summary>
    /// Finds and expires all pending bookings that have passed their expiration time.
    /// 
    /// Process:
    /// 1. Query for all pending bookings where ExpiresAt <= now
    /// 2. For each booking, call Expire() which releases reserved tickets
    /// 3. Save all changes in a single transaction
    /// 
    /// Note: Uses a new DbContext scope for each poll to avoid stale data issues.
    /// </summary>
    private async Task ProcessExpiredBookingsAsync(CancellationToken cancellationToken)
    {
        // Create a new scope for this operation (required for scoped services in background tasks)
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();

        var now = _timeProvider.UtcNow;

        // Find all pending bookings that have expired
        // Include related data needed for ticket release operations
        var expiredPendingBookings = await dbContext.Bookings
            .Where(b => b.Status == BookingStatus.Pending && b.ExpiresAt <= now)
            .Include(b => b.Items)
            .Include(b => b.Event)
                .ThenInclude(e => e.TicketTypes)
            .ToListAsync(cancellationToken);

        if (expiredPendingBookings.Count == 0)
        {
            return;  // Nothing to do
        }

        // Expire each booking (this releases reserved tickets back to available)
        foreach (var booking in expiredPendingBookings)
        {
            booking.Expire(now);
            cancellationToken.ThrowIfCancellationRequested();
        }

        // Persist all changes in a single transaction
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Marked {Count} pending bookings as expired.", expiredPendingBookings.Count);
    }
}

