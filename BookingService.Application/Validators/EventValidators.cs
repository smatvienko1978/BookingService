using BookingService.Application.DTOs;
using FluentValidation;

namespace BookingService.Application.Validators;

/// <summary>
/// Validates event creation requests from organizers.
/// Ensures all required fields are present and dates are logical.
/// </summary>
public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(100).WithMessage("Category cannot exceed 100 characters.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required.")
            .MaximumLength(500).WithMessage("Location cannot exceed 500 characters.");

        // Event must start in the future
        RuleFor(x => x.StartAt)
            .GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("Event start date must be in the future.");

        // Event must end after it starts
        RuleFor(x => x.EndAt)
            .GreaterThan(x => x.StartAt)
            .WithMessage("Event end date must be after start date.");

        // At least one ticket type is required
        RuleFor(x => x.TicketTypes)
            .NotEmpty()
            .WithMessage("At least one ticket type is required.");

        // Validate each ticket type
        RuleForEach(x => x.TicketTypes)
            .SetValidator(new TicketTypeCreateDtoValidator());
    }
}

/// <summary>
/// Validates ticket type creation data.
/// </summary>
public class TicketTypeCreateDtoValidator : AbstractValidator<TicketTypeCreateDto>
{
    public TicketTypeCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ticket type name is required.")
            .MaximumLength(100).WithMessage("Ticket type name cannot exceed 100 characters.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.")
            .LessThanOrEqualTo(100000).WithMessage("Price cannot exceed 100,000.");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be at least 1.")
            .LessThanOrEqualTo(100000).WithMessage("Capacity cannot exceed 100,000.");
    }
}

/// <summary>
/// Validates event update requests from organizers.
/// </summary>
public class UpdateEventRequestValidator : AbstractValidator<UpdateEventRequest>
{
    public UpdateEventRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(100).WithMessage("Category cannot exceed 100 characters.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required.")
            .MaximumLength(500).WithMessage("Location cannot exceed 500 characters.");

        RuleFor(x => x.EndAt)
            .GreaterThan(x => x.StartAt)
            .WithMessage("Event end date must be after start date.");

        RuleForEach(x => x.TicketTypes)
            .SetValidator(new TicketTypeUpdateDtoValidator());
    }
}

/// <summary>
/// Validates ticket type update data.
/// </summary>
public class TicketTypeUpdateDtoValidator : AbstractValidator<TicketTypeUpdateDto>
{
    public TicketTypeUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Ticket type ID is required for updates.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ticket type name is required.")
            .MaximumLength(100).WithMessage("Ticket type name cannot exceed 100 characters.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.")
            .LessThanOrEqualTo(100000).WithMessage("Price cannot exceed 100,000.");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be at least 1.")
            .LessThanOrEqualTo(100000).WithMessage("Capacity cannot exceed 100,000.");
    }
}
