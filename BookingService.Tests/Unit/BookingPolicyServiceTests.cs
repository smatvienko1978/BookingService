using BookingService.Application.Interfaces;
using BookingService.Application.Services;
using BookingService.Core.Entities;
using BookingService.Core.Enums;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace BookingService.Tests.Unit;

public class BookingPolicyServiceTests
{
    private readonly Mock<ITimeProvider> _timeProviderMock = new();
    private readonly BookingPolicyService _sut;

    public BookingPolicyServiceTests()
    {
        _timeProviderMock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);
        _sut = new BookingPolicyService(
            Options.Create(new BookingOptions { RefundCutoffHours = 24 }),
            _timeProviderMock.Object);
    }

    [Fact]
    public void EvaluateCancellation_PendingBooking_ShouldAllowWithNoRefund()
    {
        var (booking, evt) = CreateBookingAndEvent(BookingStatus.Pending, hoursUntilEvent: 48);

        var result = _sut.EvaluateCancellation(booking, evt);

        result.Allowed.Should().BeTrue();
        result.RefundAmount.Should().Be(0);
        result.DenialReason.Should().BeNull();
    }

    [Fact]
    public void EvaluateCancellation_ConfirmedBooking_MoreThan24hBeforeEvent_ShouldAllowWithFullRefund()
    {
        var (booking, evt) = CreateBookingAndEvent(BookingStatus.Confirmed, hoursUntilEvent: 48);

        var result = _sut.EvaluateCancellation(booking, evt);

        result.Allowed.Should().BeTrue();
        result.RefundAmount.Should().Be(booking.TotalAmount);
        result.DenialReason.Should().BeNull();
    }

    [Fact]
    public void EvaluateCancellation_ConfirmedBooking_Within24hOfEvent_ShouldDeny()
    {
        var (booking, evt) = CreateBookingAndEvent(BookingStatus.Confirmed, hoursUntilEvent: 12);

        var result = _sut.EvaluateCancellation(booking, evt);

        result.Allowed.Should().BeFalse();
        result.RefundAmount.Should().Be(0);
        result.DenialReason.Should().Contain("24 hours");
    }

    [Fact]
    public void EvaluateCancellation_ConfirmedBooking_Exactly24hBeforeEvent_ShouldDeny()
    {
        var (booking, evt) = CreateBookingAndEvent(BookingStatus.Confirmed, hoursUntilEvent: 24);

        var result = _sut.EvaluateCancellation(booking, evt);

        result.Allowed.Should().BeFalse();
    }

    [Fact]
    public void EvaluateCancellation_CancelledBooking_ShouldDeny()
    {
        var (booking, evt) = CreateBookingAndEvent(BookingStatus.Cancelled, hoursUntilEvent: 48);

        var result = _sut.EvaluateCancellation(booking, evt);

        result.Allowed.Should().BeFalse();
        result.DenialReason.Should().Contain("already cancelled");
    }

    [Fact]
    public void EvaluateCancellation_ExpiredBooking_ShouldDeny()
    {
        var (booking, evt) = CreateBookingAndEvent(BookingStatus.Expired, hoursUntilEvent: 48);

        var result = _sut.EvaluateCancellation(booking, evt);

        result.Allowed.Should().BeFalse();
        result.DenialReason.Should().Contain("expired");
    }

    private (Booking booking, Event evt) CreateBookingAndEvent(BookingStatus status, int hoursUntilEvent)
    {
        var now = _timeProviderMock.Object.UtcNow;
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = UserRole.Customer,
            CreatedAt = now,
            UpdatedAt = now
        };

        var evt = new Event
        {
            Id = eventId,
            OrganizerId = Guid.NewGuid(),
            Title = "Test Event",
            Description = "Description",
            Category = "Concert",
            Location = "Venue",
            StartAt = now.AddHours(hoursUntilEvent),
            EndAt = now.AddHours(hoursUntilEvent + 2),
            Status = EventStatus.Published,
            CreatedAt = now,
            UpdatedAt = now
        };

        var booking = new Booking(
            userId,
            eventId,
            status,
            now.AddDays(-1),
            now.AddMinutes(15),
            status == BookingStatus.Confirmed ? now : null,
            status == BookingStatus.Cancelled ? now : null,
            status == BookingStatus.Cancelled ? "Test cancellation" : null,
            100m,
            user,
            evt,
            null,
            []
        );

        return (booking, evt);
    }
}
