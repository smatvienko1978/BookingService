using BookingService.Application.DTOs;
using BookingService.Core.Enums;
using FluentValidation;

namespace BookingService.Application.Validators;

/// <summary>
/// Validates user registration requests.
/// Enforces password complexity and email format requirements.
/// </summary>
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        // Password complexity requirements
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(100).WithMessage("Password cannot exceed 100 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200).WithMessage("Full name cannot exceed 200 characters.");

        // Role must be a valid enum value (Customer or Organizer for self-registration)
        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role specified.")
            .Must(role => role == UserRole.Customer || role == UserRole.Organizer)
            .WithMessage("Self-registration is only allowed for Customer or Organizer roles.");
    }
}

/// <summary>
/// Validates login requests.
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
