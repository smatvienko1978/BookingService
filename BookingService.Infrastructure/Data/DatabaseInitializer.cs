using BookingService.Core.Entities;
using BookingService.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookingService.Infrastructure.Data;

/// <summary>
/// Handles database initialization including migrations and seed data.
/// </summary>
public class DatabaseInitializer(
    BookingDbContext context,
    ILogger<DatabaseInitializer> logger)
{
    private readonly BookingDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ILogger<DatabaseInitializer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Initializes the database by applying migrations and seeding data in non-production environments.
    /// </summary>
    /// <param name="isDevelopment">Whether the application is running in development mode.</param>
    public async Task InitializeAsync(bool isDevelopment)
    {
        try
        {
            _logger.LogInformation("Starting database initialization...");

            // Apply pending migrations (creates tables if they don't exist)
            await _context.Database.MigrateAsync();
            _logger.LogInformation("Database migrations applied successfully.");

            // Seed test data only in development environment
            if (isDevelopment)
            {
                await SeedDevelopmentDataAsync();
            }

            _logger.LogInformation("Database initialization completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }

    /// <summary>
    /// Seeds test data for development environment.
    /// Creates test users with different roles, sample events, and ticket types.
    /// </summary>
    private async Task SeedDevelopmentDataAsync()
    {
        // Check if data already exists to avoid duplicates
        if (await _context.Users.AnyAsync())
        {
            _logger.LogInformation("Database already contains data. Skipping seed.");
            return;
        }

        _logger.LogInformation("Seeding development test data...");

        var now = DateTimeOffset.UtcNow;

        // ============================================================
        // TEST USERS
        // ============================================================
        // All test users have password: "Password123!"
        // BCrypt hash for "Password123!" (cost factor 11)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123!");

        // Admin user - has full access to user management
        var adminUser = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = "admin@bookingservice.com",
            PasswordHash = passwordHash,
            FullName = "System Administrator",
            Role = UserRole.Admin,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Organizer user - can create and manage events
        var organizerUser = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Email = "organizer@bookingservice.com",
            PasswordHash = passwordHash,
            FullName = "Event Organizer",
            Role = UserRole.Organizer,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Second organizer for testing multi-organizer scenarios
        var organizer2User = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222223"),
            Email = "organizer2@bookingservice.com",
            PasswordHash = passwordHash,
            FullName = "Second Organizer",
            Role = UserRole.Organizer,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Customer user - can browse events and make bookings
        var customerUser = new User
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Email = "customer@bookingservice.com",
            PasswordHash = passwordHash,
            FullName = "Test Customer",
            Role = UserRole.Customer,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Second customer for testing booking isolation
        var customer2User = new User
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333334"),
            Email = "customer2@bookingservice.com",
            PasswordHash = passwordHash,
            FullName = "Second Customer",
            Role = UserRole.Customer,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Users.AddRange(adminUser, organizerUser, organizer2User, customerUser, customer2User);

        // ============================================================
        // TEST EVENTS
        // ============================================================

        // Event 1: Published concert (7 days from now) - available for booking
        var concertEventId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var concertEvent = new Event
        {
            Id = concertEventId,
            OrganizerId = organizerUser.Id,
            Title = "Summer Rock Festival 2026",
            Description = "An amazing outdoor rock concert featuring top bands. Join us for a night of incredible music under the stars!",
            Category = "Concert",
            Location = "Central Park Arena, New York",
            StartAt = now.AddDays(7),
            EndAt = now.AddDays(7).AddHours(5),
            Status = EventStatus.Published,
            CreatedAt = now,
            UpdatedAt = now,
            TicketTypes = new List<TicketType>
            {
                // VIP tickets - limited availability, premium price
                TicketType.Create(concertEventId, "VIP Front Row", 250.00m, 50),
                // Standard tickets - good availability, mid-range price
                TicketType.Create(concertEventId, "Standard", 75.00m, 500),
                // Economy tickets - high availability, budget-friendly
                TicketType.Create(concertEventId, "General Admission", 35.00m, 1000)
            }
        };

        // Event 2: Published tech conference (14 days from now)
        var conferenceEventId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var conferenceEvent = new Event
        {
            Id = conferenceEventId,
            OrganizerId = organizerUser.Id,
            Title = "Tech Innovation Summit 2026",
            Description = "Two-day conference covering AI, cloud computing, and emerging technologies. Network with industry leaders and learn from expert speakers.",
            Category = "Conference",
            Location = "Convention Center, San Francisco",
            StartAt = now.AddDays(14),
            EndAt = now.AddDays(15).AddHours(6),
            Status = EventStatus.Published,
            CreatedAt = now,
            UpdatedAt = now,
            TicketTypes = new List<TicketType>
            {
                TicketType.Create(conferenceEventId, "Full Access Pass", 500.00m, 200),
                TicketType.Create(conferenceEventId, "Day Pass", 150.00m, 300),
                TicketType.Create(conferenceEventId, "Virtual Attendance", 50.00m, 1000)
            }
        };

        // Event 3: Draft event (not visible to customers) - for testing organizer workflow
        var draftEventId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var draftEvent = new Event
        {
            Id = draftEventId,
            OrganizerId = organizerUser.Id,
            Title = "Upcoming Workshop (Draft)",
            Description = "This event is still being planned and is not yet published.",
            Category = "Workshop",
            Location = "TBD",
            StartAt = now.AddDays(30),
            EndAt = now.AddDays(30).AddHours(3),
            Status = EventStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
            TicketTypes = new List<TicketType>
            {
                TicketType.Create(draftEventId, "Workshop Seat", 100.00m, 30)
            }
        };

        // Event 4: Event by second organizer - for testing organizer isolation
        var sportsEventId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var sportsEvent = new Event
        {
            Id = sportsEventId,
            OrganizerId = organizer2User.Id,
            Title = "Championship Basketball Game",
            Description = "Watch the finals of the regional basketball championship!",
            Category = "Sports",
            Location = "Sports Arena, Chicago",
            StartAt = now.AddDays(10),
            EndAt = now.AddDays(10).AddHours(3),
            Status = EventStatus.Published,
            CreatedAt = now,
            UpdatedAt = now,
            TicketTypes = new List<TicketType>
            {
                TicketType.Create(sportsEventId, "Courtside", 300.00m, 20),
                TicketType.Create(sportsEventId, "Lower Bowl", 100.00m, 200),
                TicketType.Create(sportsEventId, "Upper Bowl", 40.00m, 500)
            }
        };

        // Event 5: Event happening soon (within 24 hours) - for testing refund cutoff
        var urgentEventId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var urgentEvent = new Event
        {
            Id = urgentEventId,
            OrganizerId = organizerUser.Id,
            Title = "Tonight's Comedy Show",
            Description = "Stand-up comedy night featuring local comedians.",
            Category = "Entertainment",
            Location = "Comedy Club, Los Angeles",
            StartAt = now.AddHours(12), // Within 24h - no refunds allowed
            EndAt = now.AddHours(14),
            Status = EventStatus.Published,
            CreatedAt = now.AddDays(-7),
            UpdatedAt = now,
            TicketTypes = new List<TicketType>
            {
                TicketType.Create(urgentEventId, "Standard Seat", 25.00m, 100)
            }
        };

        _context.Events.AddRange(concertEvent, conferenceEvent, draftEvent, sportsEvent, urgentEvent);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Development test data seeded successfully.");
        _logger.LogInformation("Test credentials - Email: admin@bookingservice.com / organizer@bookingservice.com / customer@bookingservice.com, Password: Password123!");
    }
}
