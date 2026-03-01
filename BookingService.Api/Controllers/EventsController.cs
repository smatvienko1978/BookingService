using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(IEventsService eventsService) : ControllerBase
{
    private readonly IEventsService _service = eventsService ?? throw new ArgumentNullException(nameof(eventsService));

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventSummaryDto>>> GetEvents(CancellationToken cancellationToken)
    {
        var events = await _service.GetPublished(cancellationToken);
        return Ok(events);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EventDetailDto>> GetEvent(Guid id, CancellationToken cancellationToken)
    {
        var evt = await _service.GetById(id, cancellationToken);
        return evt == null ? NotFound() : Ok(evt);
    }
}
