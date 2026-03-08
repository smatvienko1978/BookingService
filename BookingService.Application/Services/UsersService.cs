using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using BookingService.Core.Entities;
using BookingService.Core.Exceptions;
using BookingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Application.Services;

public class UsersService(BookingDbContext context, ITimeProvider timeProvider) : IUsersService
{
    private readonly BookingDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ITimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<IEnumerable<UserDto>> GetAll(CancellationToken cancellationToken = default)
    {
        var users = await _context.Users.ToListAsync(cancellationToken);
        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto?> GetById(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto> Create(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
            throw new ConflictException("Email already registered.", "DuplicateEmail");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            FullName = request.FullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            CreatedAt = _timeProvider.UtcNow,
            UpdatedAt = _timeProvider.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(user);
    }

    public async Task<bool> Update(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return false;

        user.Email = request.Email;
        user.FullName = request.FullName;
        user.UpdatedAt = _timeProvider.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Delete(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static UserDto MapToDto(User user) => new(
        user.Id,
        user.Email,
        user.FullName,
        user.Role.ToString(),
        user.CreatedAt
    );
}
