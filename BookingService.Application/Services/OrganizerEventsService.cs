using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using BookingService.Core.Entities;
using BookingService.Core.Enums;
using BookingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Application.Services;

public class OrganizerEventsService(BookingDbContext context, ITimeProvider timeProvider) : IOrganizerEventsService
{
    private readonly BookingDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ITimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<IEnumerable<EventDetailDto>> GetByOrganizer(Guid organizerId, CancellationToken cancellationToken = default)
    {
        var events = await _context.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.OrganizerId == organizerId)
            .ToListAsync(cancellationToken);

        return events.Select(MapToDetailDto).ToList();
    }

    public async Task<EventDetailDto?> GetById(Guid eventId, Guid organizerId, CancellationToken cancellationToken = default)
    {
        var evt = await _context.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == organizerId, cancellationToken);

        return evt == null ? null : MapToDetailDto(evt);
    }

    public async Task<EventStatsDto?> GetStats(Guid eventId, Guid organizerId, CancellationToken cancellationToken = default)
    {
        var evt = await _context.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == organizerId, cancellationToken);

        if (evt == null)
            return null;

        var confirmedBookings = await _context.Bookings
            .Include(b => b.Items)
            .Where(b => b.EventId == eventId && b.Status == BookingStatus.Confirmed)
            .ToListAsync(cancellationToken);

        var cancelledCount = await _context.Bookings
            .CountAsync(b => b.EventId == eventId && b.Status == BookingStatus.Cancelled, cancellationToken);

        var totalRevenue = confirmedBookings.Sum(b => b.TotalAmount);

        var ticketsSoldPerType = evt.TicketTypes
            .Select(t => new TicketTypeStatsDto(
                t.Id,
                t.Name,
                t.Capacity,
                t.SoldQuantity,
                t.ReservedQuantity,
                t.Available
            ))
            .ToList();

        var totalCapacity = evt.TicketTypes.Sum(t => t.Capacity);
        var totalSold = evt.TicketTypes.Sum(t => t.SoldQuantity);
        var occupancyRate = totalCapacity > 0 ? (double)totalSold / totalCapacity : 0;

        return new EventStatsDto(
            evt.Id,
            evt.Title,
            totalRevenue,
            occupancyRate,
            cancelledCount,
            ticketsSoldPerType
        );
    }

    public async Task<EventDetailDto> Create(Guid organizerId, CreateEventRequest request, CancellationToken cancellationToken = default)
    {
        var eventId = Guid.NewGuid();
        var evt = new Event
        {
            Id = eventId,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Location = request.Location,
            OrganizerId = organizerId,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Status = EventStatus.Draft,
            TicketTypes = request.TicketTypes
                .Select(t => TicketType.Create(eventId, t.Name, t.Price, t.Capacity))
                .ToList()
        };

        _context.Events.Add(evt);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDetailDto(evt);
    }

    public async Task<bool> Update(Guid eventId, Guid organizerId, UpdateEventRequest request, CancellationToken cancellationToken = default)
    {
        var evt = await _context.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == organizerId, cancellationToken);

        if (evt == null)
            return false;

        evt.Title = request.Title;
        evt.Description = request.Description;
        evt.Category = request.Category;
        evt.Location = request.Location;
        evt.StartAt = request.StartAt;
        evt.EndAt = request.EndAt;
        evt.UpdatedAt = _timeProvider.UtcNow;

        foreach (var tt in request.TicketTypes)
        {
            var existing = evt.TicketTypes.FirstOrDefault(t => t.Id == tt.Id);
            if (existing != null)
            {
                existing.ChangeCapacity(tt.Capacity);
                existing.UpdateDetails(tt.Name, tt.Price);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Delete(Guid eventId, Guid organizerId, CancellationToken cancellationToken = default)
    {
        var evt = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == organizerId, cancellationToken);

        if (evt == null)
            return false;

        _context.Events.Remove(evt);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static EventDetailDto MapToDetailDto(Event evt) => new(
        evt.Id,
        evt.Title,
        evt.Description,
        evt.Category,
        evt.Location,
        evt.OrganizerId,
        evt.StartAt,
        evt.EndAt,
        evt.Status.ToString(),
        evt.TicketTypes
            .Select(t => new TicketTypeAvailabilityDto(
                t.Id,
                t.Name,
                t.Price,
                t.Capacity,
                t.ReservedQuantity,
                t.Available
            ))
            .ToList()
    );
}
