using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using BookingService.Core.Entities;
using BookingService.Core.Enums;
using BookingService.Core.Request;
using BookingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookingService.Application.Services;

public class BookingsService(
    BookingDbContext context,
    IOptions<BookingOptions> options,
    IBookingPolicyService policyService,
    ITimeProvider timeProvider) : IBookingsService
{
    private readonly BookingDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly BookingOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly IBookingPolicyService _policyService = policyService ?? throw new ArgumentNullException(nameof(policyService));
    private readonly ITimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<BookingDto> Create(Guid userId, CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
            throw new ArgumentException("At least one ticket item is required.", nameof(request));

        var user = await _context.Users.FindAsync([userId], cancellationToken)
            ?? throw new InvalidOperationException($"User '{userId}' not found.");

        var evt = await _context.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken)
            ?? throw new InvalidOperationException($"Event '{request.EventId}' not found.");

        var now = _timeProvider.UtcNow;
        var expiresAt = now.AddMinutes(_options.TimeoutMinutes);

        var booking = new Booking(userId, request.EventId, expiresAt, user, evt);

        foreach (var item in request.Items)
        {
            var ticketType = evt.TicketTypes.FirstOrDefault(t => t.Id == item.TicketTypeId)
                ?? throw new InvalidOperationException($"Ticket type '{item.TicketTypeId}' not found for event.");
            booking.AddItem(ticketType, item.Quantity);
        }

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(booking);
    }

    public async Task<IEnumerable<BookingDto>> GetByUser(Guid userId, CancellationToken cancellationToken = default)
    {
        var bookings = await _context.Bookings
            .Where(x => x.UserId == userId)
            .Include(b => b.Event)
            .Include(b => b.Items)
            .Include(b => b.Refund)
            .ToListAsync(cancellationToken);

        return [.. bookings.Select(MapToDto)];
    }

    public async Task<BookingDto?> GetById(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings
            .Where(b => b.Id == bookingId)
            .Include(b => b.Event)
            .Include(b => b.Items)
            .Include(b => b.Refund)
            .FirstOrDefaultAsync(cancellationToken);

        return booking == null ? null : MapToDto(booking);
    }

    public async Task Confirm(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings
            .Include(b => b.Items)
            .Include(b => b.Event)
                .ThenInclude(e => e.TicketTypes)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        ArgumentNullException.ThrowIfNull(booking, nameof(booking));

        var now = _timeProvider.UtcNow;
        
        if (booking.Status != BookingStatus.Pending)
            throw new InvalidOperationException("Booking cannot be confirmed.");

        if (booking.ExpiresAt < now)
            throw new InvalidOperationException("Booking expired.");

        booking.Confirm(now);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<BookingDto> Cancel(Guid bookingId, Guid userId, string? reason, CancellationToken cancellationToken = default)
    {
        var booking = await _context.Bookings
            .Include(b => b.Items)
            .Include(b => b.Event)
                .ThenInclude(e => e.TicketTypes)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken) ?? throw new InvalidOperationException($"Booking '{bookingId}' not found.");
        
        if (booking.UserId != userId)
            throw new UnauthorizedAccessException("You can only cancel your own bookings.");

        var result = _policyService.EvaluateCancellation(booking, booking.Event);

        if (!result.Allowed)
            throw new InvalidOperationException(result.DenialReason ?? "Cancellation not allowed.");

        var now = _timeProvider.UtcNow;
        booking.Cancel(now, reason ?? "User requested cancellation");

        if (result.RefundAmount > 0)
        {
            var refund = new Refund
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                Amount = result.RefundAmount,
                CreatedAt = now,
                Reason = reason ?? "Cancellation - full refund (more than 24h before event)",
                Status = RefundStatus.Completed
            };
            _context.Refunds.Add(refund);
            booking.Refund = refund;
        }

        await _context.SaveChangesAsync(cancellationToken);
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
