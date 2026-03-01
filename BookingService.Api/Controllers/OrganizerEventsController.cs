using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using BookingService.Core.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Api.Controllers;

[Authorize(Roles = "Organizer")]
[ApiController]
[Route("api/organizer/events")]
public class OrganizerEventsController(IOrganizerEventsService organizerEventsService) : ControllerBase
{
    private readonly IOrganizerEventsService _service = organizerEventsService ?? throw new ArgumentNullException(nameof(organizerEventsService));

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDetailDto>>> GetMyEvents(CancellationToken cancellationToken)
    {
        var organizerId = User.GetUserId();
        var events = await _service.GetByOrganizer(organizerId, cancellationToken);
        return Ok(events);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EventDetailDto>> GetEvent(Guid id, CancellationToken cancellationToken)
    {
        var organizerId = User.GetUserId();
        var evt = await _service.GetById(id, organizerId, cancellationToken);
        return evt == null ? NotFound() : Ok(evt);
    }

    [HttpGet("{id}/stats")]
    public async Task<ActionResult<EventStatsDto>> GetEventStats(Guid id, CancellationToken cancellationToken)
    {
        var organizerId = User.GetUserId();
        var stats = await _service.GetStats(id, organizerId, cancellationToken);
        return stats == null ? NotFound() : Ok(stats);
    }

    [HttpPost]
    public async Task<ActionResult<EventDetailDto>> CreateEvent(CreateEventRequest request, CancellationToken cancellationToken)
    {
        var organizerId = User.GetUserId();
        var evt = await _service.Create(organizerId, request, cancellationToken);
        return CreatedAtAction(nameof(GetEvent), new { id = evt.Id }, evt);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(Guid id, UpdateEventRequest request, CancellationToken cancellationToken)
    {
        var organizerId = User.GetUserId();
        var updated = await _service.Update(id, organizerId, request, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken cancellationToken)
    {
        var organizerId = User.GetUserId();
        var deleted = await _service.Delete(id, organizerId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
