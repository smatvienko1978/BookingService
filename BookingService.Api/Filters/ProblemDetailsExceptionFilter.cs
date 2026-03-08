using System.Net;
using BookingService.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Api.Filters;

/// <summary>
/// Maps exceptions to RFC 7807 ProblemDetails and appropriate HTTP status codes.
/// Ensures consistent error responses (400, 403, 404, 409, 500) without try/catch in controllers.
/// </summary>
public sealed class ProblemDetailsExceptionFilter : IExceptionFilter
{
    private const string DefaultTitle = "An error occurred.";
    private readonly IHostEnvironment _environment;

    public ProblemDetailsExceptionFilter(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.ExceptionHandled) return;

        var (statusCode, title, detail, errorCode) = MapException(context.Exception, _environment.IsDevelopment());

        var problemDetails = new ProblemDetails
        {
            Type = errorCode != null ? $"https://api.bookingservice.com/errors/{errorCode}" : null,
            Title = title,
            Detail = detail,
            Status = (int)statusCode,
            Instance = context.HttpContext.Request.Path,
            Extensions =
            {
                ["traceId"] = context.HttpContext.TraceIdentifier
            }
        };

        if (errorCode != null)
            problemDetails.Extensions["errorCode"] = errorCode;

        context.Result = new ObjectResult(problemDetails) { StatusCode = (int)statusCode };
        context.ExceptionHandled = true;
    }

    private static (HttpStatusCode statusCode, string title, string? detail, string? errorCode) MapException(Exception ex, bool isDevelopment)
    {
        return ex switch
        {
            NotFoundException n => (HttpStatusCode.NotFound, "Not Found", n.Message, n.ErrorCode),
            CapacityExceededException ce => (HttpStatusCode.Conflict, "Capacity Exceeded", ce.Message, ce.ErrorCode),
            ConflictException c => (HttpStatusCode.Conflict, "Conflict", c.Message, c.ErrorCode),
            InvalidBookingStateException ib => (HttpStatusCode.BadRequest, "Invalid Booking State", ib.Message, ib.ErrorCode),
            ValidationException v => (HttpStatusCode.BadRequest, "Validation Error", v.Message, v.ErrorCode),
            UnauthorizedAccessException _ => (HttpStatusCode.Forbidden, "Forbidden", ex.Message, "Forbidden"),
            DbUpdateConcurrencyException _ => (HttpStatusCode.Conflict, "Concurrency Conflict", "The resource was modified by another request. Please refresh and try again.", "ConcurrencyConflict"),
            InvalidOperationException _ => (HttpStatusCode.BadRequest, "Bad Request", ex.Message, "BadRequest"),
            ArgumentException _ => (HttpStatusCode.BadRequest, "Bad Request", ex.Message, "BadRequest"),
            KeyNotFoundException _ => (HttpStatusCode.NotFound, "Not Found", ex.Message, "NotFound"),
            _ => (HttpStatusCode.InternalServerError, DefaultTitle, isDevelopment ? ex.Message : "An unexpected error occurred.", null)
        };
    }
}
