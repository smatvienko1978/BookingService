using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using BookingService.Core.Enums;
using BookingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Application.Services;

public class EventsService(BookingDbContext context) : IEventsService
{
    private readonly BookingDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<IEnumerable<EventSummaryDto>> GetPublished(CancellationToken cancellationToken = default)
    {
        var events = await _context.Events
            .Include(e => e.TicketTypes)
            .Where(e => e.Status == EventStatus.Published)
            .ToListAsync(cancellationToken);

        return events.Select(e => new EventSummaryDto(
            e.Id,
            e.Title,
            e.Location,
            e.StartAt,
            e.EndAt,
            e.Category,
            e.TicketTypes
                .Select(t => new TicketTypeAvailabilityDto(
                    t.Id,
                    t.Name,
                    t.Price,
                    t.Capacity,
                    t.ReservedQuantity,
                    t.Available
                ))
                .ToList()
        )).ToList();
    }

    public async Task<EventDetailDto?> GetById(Guid eventId, CancellationToken cancellationToken = default)
    {
        var evt = await _context.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.Status == EventStatus.Published, cancellationToken);

        if (evt == null)
            return null;

        return new EventDetailDto(
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
}
