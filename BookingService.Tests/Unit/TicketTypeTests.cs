using BookingService.Core.Entities;
using BookingService.Core.Exceptions;
using FluentAssertions;

namespace BookingService.Tests.Unit;

public class TicketTypeTests
{
    [Fact]
    public void Create_ShouldInitializeCorrectly()
    {
        var eventId = Guid.NewGuid();

        var ticketType = TicketType.Create(eventId, "VIP", 100m, 50);

        ticketType.EventId.Should().Be(eventId);
        ticketType.Name.Should().Be("VIP");
        ticketType.Price.Should().Be(100m);
        ticketType.Capacity.Should().Be(50);
        ticketType.ReservedQuantity.Should().Be(0);
        ticketType.SoldQuantity.Should().Be(0);
        ticketType.Available.Should().Be(50);
        ticketType.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Reserve_ShouldIncreaseReservedQuantity()
    {
        var ticketType = TicketType.Create(Guid.NewGuid(), "Regular", 50m, 100);

        ticketType.Reserve(10);

        ticketType.ReservedQuantity.Should().Be(10);
        ticketType.Available.Should().Be(90);
    }

    [Fact]
    public void Reserve_WhenNotEnoughAvailable_ShouldThrow()
    {
        var ticketType = TicketType.Create(Guid.NewGuid(), "Regular", 50m, 10);

        var act = () => ticketType.Reserve(15);

        act.Should().Throw<CapacityExceededException>().WithMessage("*Not enough*");
    }

    [Fact]
    public void Confirm_ShouldMoveFromReservedToSold()
    {
        var ticketType = TicketType.Create(Guid.NewGuid(), "Regular", 50m, 100);
        ticketType.Reserve(10);

        ticketType.Confirm(10);

        ticketType.ReservedQuantity.Should().Be(0);
        ticketType.SoldQuantity.Should().Be(10);
        ticketType.Available.Should().Be(90);
    }

    [Fact]
    public void Release_ShouldDecreaseReservedQuantity()
    {
        var ticketType = TicketType.Create(Guid.NewGuid(), "Regular", 50m, 100);
        ticketType.Reserve(10);

        ticketType.Release(5);

        ticketType.ReservedQuantity.Should().Be(5);
        ticketType.Available.Should().Be(95);
    }

    [Fact]
    public void ChangeCapacity_ShouldUpdateCapacity()
    {
        var ticketType = TicketType.Create(Guid.NewGuid(), "Regular", 50m, 100);

        ticketType.ChangeCapacity(150);

        ticketType.Capacity.Should().Be(150);
        ticketType.Available.Should().Be(150);
    }

    [Fact]
    public void ChangeCapacity_WhenBelowSoldPlusReserved_ShouldThrow()
    {
        var ticketType = TicketType.Create(Guid.NewGuid(), "Regular", 50m, 100);
        ticketType.Reserve(30);
        ticketType.Confirm(20);

        var act = () => ticketType.ChangeCapacity(25);

        act.Should().Throw<InvalidOperationException>().WithMessage("*below sold/reserved*");
    }
}
