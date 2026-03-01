using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using BookingService.Core.Helpers;
using BookingService.Core.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BookingsController(IBookingsService bookingsService) : ControllerBase
{
    private readonly IBookingsService _service = bookingsService ?? throw new ArgumentNullException(nameof(bookingsService));

    /// <summary>
    /// Gets all bookings for the authenticated user with pagination.
    /// Results are ordered by creation date (newest first).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<BookingDto>>> GetUserBookings(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var pagination = new PaginationRequest(page, pageSize);
        var bookings = await _service.GetByUser(userId, pagination, cancellationToken);
        return Ok(bookings);
    }

    /// <summary>
    /// Gets a specific booking by ID. Users can only view their own bookings.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BookingDto>> GetBooking(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var booking = await _service.GetById(id, userId, cancellationToken);
        return booking == null ? NotFound() : Ok(booking);
    }

    [HttpPost]
    public async Task<ActionResult<BookingDto>> CreateBooking(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var booking = await _service.Create(userId, request, cancellationToken);
        return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
    }

    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> ConfirmBooking(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _service.Confirm(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<BookingDto>> CancelBooking(Guid id, [FromBody] CancelBookingRequest? request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        try
        {
            var booking = await _service.Cancel(id, userId, request?.Reason, cancellationToken);
            return Ok(booking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
