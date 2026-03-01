using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Api.Controllers;

/// <summary>
/// Public API for browsing events.
/// No authentication required - anyone can view published events.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EventsController(IEventsService eventsService) : ControllerBase
{
    private readonly IEventsService _service = eventsService ?? throw new ArgumentNullException(nameof(eventsService));

    /// <summary>
    /// Gets all published events with pagination.
    /// Events are ordered by start date (soonest first).
    /// Includes real-time ticket availability for each event.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<EventSummaryDto>>> GetEvents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var pagination = new PaginationRequest(page, pageSize);
        var events = await _service.GetPublished(pagination, cancellationToken);
        return Ok(events);
    }

    /// <summary>
    /// Gets detailed information about a specific event.
    /// Returns 404 if event doesn't exist or is not published.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EventDetailDto>> GetEvent(Guid id, CancellationToken cancellationToken)
    {
        var evt = await _service.GetById(id, cancellationToken);
        return evt == null ? NotFound() : Ok(evt);
    }
}
