using BookingService.Core.Request;
using FluentValidation;

namespace BookingService.Application.Validators;

/// <summary>
/// Validates booking creation requests.
/// Ensures all required fields are present and have valid values
/// before the request reaches the service layer.
/// </summary>
public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        // Event ID must be provided
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required.");

        // At least one ticket item must be included
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one ticket item is required.");

        // Validate each item in the booking
        RuleForEach(x => x.Items)
            .SetValidator(new BookingItemRequestValidator());
    }
}

/// <summary>
/// Validates individual booking items (ticket type + quantity).
/// </summary>
public class BookingItemRequestValidator : AbstractValidator<BookingItemRequest>
{
    public BookingItemRequestValidator()
    {
        RuleFor(x => x.TicketTypeId)
            .NotEmpty()
            .WithMessage("Ticket type ID is required.");

        // Quantity must be positive and reasonable
        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be at least 1.")
            .LessThanOrEqualTo(100)
            .WithMessage("Cannot book more than 100 tickets of the same type in a single booking.");
    }
}
