using BookingService.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Data;

/// <summary>
/// Entity Framework Core database context for the booking system.
/// </summary>
public class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Users table.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Events table.
    /// </summary>
    public DbSet<Event> Events => Set<Event>();

    /// <summary>
    /// Ticket types table.
    /// </summary>
    public DbSet<TicketType> TicketTypes => Set<TicketType>();

    /// <summary>
    /// Bookings table.
    /// </summary>
    public DbSet<Booking> Bookings => Set<Booking>();

    /// <summary>
    /// Booking items table.
    /// </summary>
    public DbSet<BookingItem> BookingItems => Set<BookingItem>();

    /// <summary>
    /// Refunds table.
    /// </summary>
    public DbSet<Refund> Refunds => Set<Refund>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);
    }
}
