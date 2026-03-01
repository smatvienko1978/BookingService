using BookingService.Core.Entities;
using BookingService.Core.Enums;
using FluentAssertions;

namespace BookingService.Tests.Unit;

public class BookingTests
{
    [Fact]
    public void AddItem_ShouldAddItemAndUpdateTotal()
    {
        var (booking, ticketType) = CreateBookingWithTicketType();

        booking.AddItem(ticketType, 2);

        booking.Items.Should().HaveCount(1);
        booking.Items.First().Quantity.Should().Be(2);
        booking.TotalAmount.Should().Be(200m);
        ticketType.ReservedQuantity.Should().Be(2);
    }

    [Fact]
    public void AddItem_MultipleTypes_ShouldAccumulateTotal()
    {
        var (booking, vipTicket) = CreateBookingWithTicketType(price: 100m);
        var regularTicket = TicketType.Create(booking.EventId, "Regular", 50m, 100);

        booking.AddItem(vipTicket, 2);
        booking.AddItem(regularTicket, 3);

        booking.Items.Should().HaveCount(2);
        booking.TotalAmount.Should().Be(350m);
    }

    [Fact]
    public void Confirm_ShouldChangeStatusAndSetConfirmedAt()
    {
        var (booking, ticketType) = CreateBookingWithTicketType();
        booking.AddItem(ticketType, 2);
        var now = DateTimeOffset.UtcNow;

        booking.Confirm(now);

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ConfirmedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Cancel_ShouldReleaseTicketsAndSetStatus()
    {
        var (booking, ticketType) = CreateBookingWithTicketType();
        booking.AddItem(ticketType, 5);
        var initialReserved = ticketType.ReservedQuantity;

        booking.Cancel(DateTimeOffset.UtcNow, "User requested");

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancellationReason.Should().Be("User requested");
        ticketType.ReservedQuantity.Should().Be(initialReserved - 5);
    }

    [Fact]
    public void Expire_ShouldReleaseTicketsAndSetStatus()
    {
        var (booking, ticketType) = CreateBookingWithTicketType(expiresInMinutes: -5);
        booking.AddItem(ticketType, 3);

        booking.Expire(DateTimeOffset.UtcNow);

        booking.Status.Should().Be(BookingStatus.Expired);
        ticketType.ReservedQuantity.Should().Be(0);
    }

    private static (Booking booking, TicketType ticketType) CreateBookingWithTicketType(
        decimal price = 100m,
        int expiresInMinutes = 15)
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = UserRole.Customer,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var evt = new Event
        {
            Id = eventId,
            OrganizerId = Guid.NewGuid(),
            Title = "Test Event",
            Description = "Description",
            Category = "Concert",
            Location = "Venue",
            StartAt = DateTimeOffset.UtcNow.AddDays(7),
            EndAt = DateTimeOffset.UtcNow.AddDays(7).AddHours(2),
            Status = EventStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var ticketType = TicketType.Create(eventId, "VIP", price, 100);
        evt.TicketTypes.Add(ticketType);

        var booking = new Booking(
            userId,
            eventId,
            DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes),
            user,
            evt
        );

        return (booking, ticketType);
    }
}
