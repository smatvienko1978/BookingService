using BookingService.Core.Enums;

namespace BookingService.Core.Entities;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier of the user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User's email address (used for login).
    /// </summary>
    public string Email { get; set; } = default!;

    /// <summary>
    /// BCrypt hash of the user's password.
    /// </summary>
    public string PasswordHash { get; set; } = default!;

    /// <summary>
    /// User's full name.
    /// </summary>
    public string FullName { get; set; } = default!;

    /// <summary>
    /// User's role (Customer, Organizer, Admin).
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// When the user account was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the user account was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Collection of events organized by this user (if Organizer role).
    /// </summary>
    public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();

    /// <summary>
    /// Collection of bookings made by this user.
    /// </summary>
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
