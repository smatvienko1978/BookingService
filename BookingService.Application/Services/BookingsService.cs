using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using BookingService.Core.Entities;
using BookingService.Core.Enums;
using BookingService.Core.Exceptions;
using BookingService.Core.Request;
using BookingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookingService.Application.Services;

/// <summary>
/// Core service for managing ticket bookings.
/// Orchestrates the booking lifecycle: creation, confirmation, cancellation.
/// 
/// Key responsibilities:
/// - Creating pending bookings with ticket reservations
/// - Confirming bookings (simulates payment completion)
/// - Cancelling bookings with policy-based refund evaluation
/// - Retrieving booking history for users
/// 
/// Uses ITimeProvider for testability - all time-dependent operations
/// use injected time instead of DateTime.Now.
/// </summary>
public class BookingsService(
    BookingDbContext context,
    IOptions<BookingOptions> options,
    IBookingPolicyService policyService,
    ITimeProvider timeProvider,
    ILogger<BookingsService> logger) : IBookingsService
{
    private readonly BookingDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly BookingOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly IBookingPolicyService _policyService = policyService ?? throw new ArgumentNullException(nameof(policyService));
    private readonly ITimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly ILogger<BookingsService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Creates a new pending booking for the specified user.
    /// 
    /// Process:
    /// 1. Validates user and event exist
    /// 2. Creates booking with expiration time (configurable timeout)
    /// 3. For each requested ticket type, reserves the specified quantity
    /// 4. Calculates total amount from all items
    /// 5. Persists to database
    /// 
    /// The booking starts in Pending status and must be confirmed before expiration.
    /// If not confirmed in time, the background worker will expire it and release tickets.
    /// </summary>
    public async Task<BookingDto> Create(Guid userId, CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
            throw new ValidationException("At least one ticket item is required.");

        var user = await _context.Users.FindAsync([userId], cancellationToken)
            ?? throw new NotFoundException($"User '{userId}' not found.");

        // Load event with ticket types for availability checking
        var evt = await _context.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken)
            ?? throw new NotFoundException($"Event '{request.EventId}' not found.");

        // Calculate expiration time based on configured timeout
        var now = _timeProvider.UtcNow;
        var expiresAt = now.AddMinutes(_options.TimeoutMinutes);

        var booking = new Booking(userId, request.EventId, expiresAt, user, evt);

        // Add each requested ticket type to the booking
        // This will reserve tickets and throw if not enough available
        foreach (var item in request.Items)
        {
            var ticketType = evt.TicketTypes.FirstOrDefault(t => t.Id == item.TicketTypeId)
                ?? throw new NotFoundException($"Ticket type '{item.TicketTypeId}' not found for event.");
            booking.AddItem(ticketType, item.Quantity);
        }

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("BookingCreated BookingId={BookingId} UserId={UserId} EventId={EventId}", booking.Id, userId, request.EventId);
        return MapToDto(booking);
    }

    /// <summary>
    /// Retrieves user's bookings with pagination.
    /// Results are ordered by creation date (newest first) for better UX.
    /// </summary>
    public async Task<PaginatedResponse<BookingDto>> GetByUser(Guid userId, PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var query = _context.Bookings
            .Where(x => x.UserId == userId)
            .OrderByDescending(b => b.CreatedAt); // Most recent first

        // Get total count for pagination metadata
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and fetch data
        var bookings = await query
            .Skip(pagination.Skip)
            .Take(pagination.ValidatedPageSize)
            .Include(b => b.Event)
            .Include(b => b.Items)
            .Include(b => b.Refund)
            .ToListAsync(cancellationToken);

        var items = bookings.Select(MapToDto).ToList();
        return PaginatedResponse<BookingDto>.Create(items, totalCount, pagination);
    }

    /// <summary>
    /// Retrieves a booking by ID with ownership verification.
    /// Users can only view their own bookings for security/privacy.
    /// </summary>
    public async Task<BookingDto?> GetById(Guid bookingId, Guid userId, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings
            .Where(b => b.Id == bookingId && b.UserId == userId) // Ownership check
            .Include(b => b.Event)
            .Include(b => b.Items)
            .Include(b => b.Refund)
            .FirstOrDefaultAsync(cancellationToken);

        return booking == null ? null : MapToDto(booking);
    }

    /// <summary>
    /// Confirms a pending booking, converting reserved tickets to sold.
    /// This simulates successful payment completion.
    /// 
    /// In a real system, this would be called after payment gateway confirmation.
    /// </summary>
    /// <exception cref="InvalidOperationException">If booking not found, not pending, or expired.</exception>
    public async Task Confirm(Guid bookingId, CancellationToken cancellationToken = default)
    {
        // Load booking with all related data needed for confirmation
        var booking = await _context.Bookings
            .Include(b => b.Items)
            .Include(b => b.Event)
                .ThenInclude(e => e.TicketTypes)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking == null)
            throw new NotFoundException($"Booking '{bookingId}' not found.");

        var now = _timeProvider.UtcNow;
        
        if (booking.Status != BookingStatus.Pending)
            throw new InvalidBookingStateException("Booking cannot be confirmed in its current state.");

        if (booking.ExpiresAt < now)
            throw new InvalidBookingStateException("Booking has expired.");

        // Confirm moves tickets from Reserved to Sold state
        booking.Confirm(now);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("BookingConfirmed BookingId={BookingId}", bookingId);
    }

    /// <summary>
    /// Cancels a booking and processes refund based on policy.
    /// 
    /// Cancellation policy (evaluated by BookingPolicyService):
    /// - Pending bookings: Always allowed, no refund (no payment made)
    /// - Confirmed bookings >24h before event: Full refund
    /// - Confirmed bookings ≤24h before event: Not allowed (no refund)
    /// 
    /// The 24-hour cutoff is configurable via BookingOptions.RefundCutoffHours.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">If user doesn't own the booking.</exception>
    /// <exception cref="InvalidOperationException">If cancellation not allowed by policy.</exception>
    public async Task<BookingDto> Cancel(Guid bookingId, Guid userId, string? reason, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings
            .Include(b => b.Items)
            .Include(b => b.Event)
                .ThenInclude(e => e.TicketTypes)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken) ?? throw new NotFoundException($"Booking '{bookingId}' not found.");
        
        // Security: Users can only cancel their own bookings
        if (booking.UserId != userId)
            throw new UnauthorizedAccessException("You can only cancel your own bookings.");

        // Evaluate cancellation policy (determines if allowed and refund amount)
        var result = _policyService.EvaluateCancellation(booking, booking.Event);

        if (!result.Allowed)
            throw new ValidationException(result.DenialReason ?? "Cancellation not allowed.");

        var now = _timeProvider.UtcNow;
        booking.Cancel(now, reason ?? "User requested cancellation");

        // Create refund record if applicable (confirmed booking cancelled >24h before event)
        if (result.RefundAmount > 0)
        {
            var refund = new Refund
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                Amount = result.RefundAmount,
                CreatedAt = now,
                Reason = reason ?? "Cancellation - full refund (more than 24h before event)",
                Status = RefundStatus.Completed  // In real system, would be Pending until processed
            };
            _context.Refunds.Add(refund);
            booking.Refund = refund;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("BookingCancelled BookingId={BookingId} UserId={UserId}", bookingId, userId);
        return MapToDto(booking);
    }

    private static BookingDto MapToDto(Booking booking) => new(
        booking.Id,
        booking.EventId,
        booking.Event.Title,
        booking.Status.ToString(),
        booking.CreatedAt,
        booking.ExpiresAt,
        booking.ConfirmedAt,
        booking.CancelledAt,
        booking.CancellationReason,
        booking.TotalAmount,
        [.. booking.Items.Select(i => new BookingItemDto(
            i.Id,
            i.TicketTypeId,
            i.TicketTypeName,
            i.Quantity,
            i.UnitPrice,
            i.TotalAmount
        ))],
        booking.Refund == null ? null : new RefundDto(
            booking.Refund.Id,
            booking.Refund.Amount,
            booking.Refund.Status.ToString(),
            booking.Refund.CreatedAt,
            booking.Refund.Reason
        )
    );
}
