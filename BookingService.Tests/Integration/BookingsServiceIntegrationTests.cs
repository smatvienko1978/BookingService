using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using BookingService.Application.Services;
using BookingService.Core.Entities;
using BookingService.Core.Enums;
using BookingService.Core.Exceptions;
using BookingService.Core.Request;
using BookingService.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace BookingService.Tests.Integration;

public class BookingsServiceIntegrationTests : IDisposable
{
    private readonly BookingDbContext _context;
    private readonly BookingsService _sut;
    private readonly User _testUser;
    private readonly Event _testEvent;
    private readonly TicketType _vipTicket;
    private readonly TicketType _regularTicket;

    public BookingsServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BookingDbContext(options);

        var bookingOptions = Options.Create(new BookingOptions
        {
            TimeoutMinutes = 15,
            RefundCutoffHours = 24,
            ExpiryPollIntervalMinutes = 1
        });
        var timeProvider = new SystemTimeProvider();
        var policyService = new BookingPolicyService(bookingOptions, timeProvider);
        var logger = new Mock<ILogger<BookingsService>>().Object;

        _sut = new BookingsService(_context, bookingOptions, policyService, timeProvider, logger);

        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "customer@test.com",
            PasswordHash = "hash",
            FullName = "Test Customer",
            Role = UserRole.Customer,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _testEvent = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = Guid.NewGuid(),
            Title = "Test Concert",
            Description = "A great concert",
            Category = "Concert",
            Location = "Stadium",
            StartAt = DateTimeOffset.UtcNow.AddDays(7),
            EndAt = DateTimeOffset.UtcNow.AddDays(7).AddHours(3),
            Status = EventStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _vipTicket = TicketType.Create(_testEvent.Id, "VIP", 150m, 50);
        _regularTicket = TicketType.Create(_testEvent.Id, "Regular", 50m, 200);

        _testEvent.TicketTypes.Add(_vipTicket);
        _testEvent.TicketTypes.Add(_regularTicket);

        _context.Users.Add(_testUser);
        _context.Events.Add(_testEvent);
        _context.SaveChanges();
    }

    [Fact]
    public async Task Create_ShouldCreatePendingBooking()
    {
        var request = new CreateBookingRequest(
            _testEvent.Id,
            [new BookingItemRequest(_vipTicket.Id, 2)]
        );

        var booking = await _sut.Create(_testUser.Id, request);

        booking.Should().NotBeNull();
        booking.Status.Should().Be("Pending");
        booking.Items.Should().HaveCount(1);
        booking.TotalAmount.Should().Be(300m);
        booking.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Create_MultipleTicketTypes_ShouldCalculateTotalCorrectly()
    {
        var request = new CreateBookingRequest(
            _testEvent.Id,
            [
                new BookingItemRequest(_vipTicket.Id, 2),
                new BookingItemRequest(_regularTicket.Id, 3)
            ]
        );

        var booking = await _sut.Create(_testUser.Id, request);

        booking.Items.Should().HaveCount(2);
        booking.TotalAmount.Should().Be(450m);
    }

    [Fact]
    public async Task Create_ShouldReserveTickets()
    {
        var initialVipAvailable = _vipTicket.Available;
        var request = new CreateBookingRequest(
            _testEvent.Id,
            [new BookingItemRequest(_vipTicket.Id, 5)]
        );

        await _sut.Create(_testUser.Id, request);

        _vipTicket.Available.Should().Be(initialVipAvailable - 5);
        _vipTicket.ReservedQuantity.Should().Be(5);
    }

    [Fact]
    public async Task Create_ExceedingCapacity_ShouldThrow()
    {
        var request = new CreateBookingRequest(
            _testEvent.Id,
            [new BookingItemRequest(_vipTicket.Id, 100)]
        );

        Func<Task> act = async () => await _sut.Create(_testUser.Id, request);

        await act.Should().ThrowAsync<BookingService.Core.Exceptions.CapacityExceededException>().WithMessage("*Not enough*");
    }

    [Fact]
    public async Task Confirm_ShouldConfirmAndMoveToSold()
    {
        var request = new CreateBookingRequest(
            _testEvent.Id,
            [new BookingItemRequest(_vipTicket.Id, 3)]
        );
        var booking = await _sut.Create(_testUser.Id, request);

        await _sut.Confirm(booking.Id);

        var confirmed = await _sut.GetById(booking.Id, _testUser.Id);
        confirmed!.Status.Should().Be("Confirmed");
        _vipTicket.SoldQuantity.Should().Be(3);
        _vipTicket.ReservedQuantity.Should().Be(0);
    }

    [Fact]
    public async Task Cancel_PendingBooking_ShouldCancelAndReleaseTickets()
    {
        var request = new CreateBookingRequest(
            _testEvent.Id,
            [new BookingItemRequest(_vipTicket.Id, 5)]
        );
        var booking = await _sut.Create(_testUser.Id, request);
        var initialAvailable = _vipTicket.Available;

        var cancelled = await _sut.Cancel(booking.Id, _testUser.Id, "Changed my mind");

        cancelled.Status.Should().Be("Cancelled");
        cancelled.Refund.Should().BeNull();
        _vipTicket.Available.Should().Be(initialAvailable + 5);
    }

    [Fact]
    public async Task Cancel_ConfirmedBooking_MoreThan24h_ShouldRefund()
    {
        var request = new CreateBookingRequest(
            _testEvent.Id,
            [new BookingItemRequest(_vipTicket.Id, 2)]
        );
        var booking = await _sut.Create(_testUser.Id, request);
        await _sut.Confirm(booking.Id);

        var cancelled = await _sut.Cancel(booking.Id, _testUser.Id, "Emergency");

        cancelled.Status.Should().Be("Cancelled");
        cancelled.Refund.Should().NotBeNull();
        cancelled.Refund!.Amount.Should().Be(300m);
        cancelled.Refund.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task Confirm_WhenBookingAlreadyCancelled_ShouldThrowInvalidBookingState()
    {
        var request = new CreateBookingRequest(_testEvent.Id, [new BookingItemRequest(_vipTicket.Id, 2)]);
        var booking = await _sut.Create(_testUser.Id, request);
        await _sut.Cancel(booking.Id, _testUser.Id, "Changed mind");

        var act = async () => await _sut.Confirm(booking.Id);

        await act.Should().ThrowAsync<InvalidBookingStateException>();
    }

    [Fact]
    public async Task GetByUser_ShouldReturnOnlyUserBookings()
    {
        var request = new CreateBookingRequest(
            _testEvent.Id,
            [new BookingItemRequest(_regularTicket.Id, 1)]
        );
        await _sut.Create(_testUser.Id, request);
        await _sut.Create(_testUser.Id, request);

        var pagination = new PaginationRequest(1, 10);
        var result = await _sut.GetByUser(_testUser.Id, pagination);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(b => b.EventId.Should().Be(_testEvent.Id));
        result.TotalCount.Should().Be(2);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
