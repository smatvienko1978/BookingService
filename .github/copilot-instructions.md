# BookingService AI Coding Agent Instructions

## Architecture Overview
BookingService is a layered .NET 9.0 ticket booking system with four projects:
- **BookingService.Core**: Domain entities and enums (no external dependencies)
- **BookingService.Infrastructure**: Entity Framework Core 9 data access layer with SQL Server
- **BookingService.Api**: ASP.NET Core 9 controller-based API with RESTful endpoints
- **BookingService.Tests**: xUnit test suite

**Dependency Flow**: Api → Infrastructure & Core, Infrastructure → Core, Core → nothing

## Domain Model & Entity Relationships
The core domain revolves around event bookings:
- **User**: Organizers create events; attendees create bookings (relationships: `OrganizedEvents`, `Bookings`)
- **Event**: Published by users, contains multiple ticket types (status: Draft → Published → Cancelled)
- **TicketType**: Defines price, capacity, and active status per event
- **Booking**: User purchases with status: Pending → Confirmed → Cancelled/Expired
- **BookingItem**: Individual tickets within a booking (links to TicketType)
- **Refund**: Associated with bookings (status: Pending/Processed/Failed)

**Key Constraint**: Foreign keys use `DeleteBehavior.Restrict` to prevent orphaned records—never cascade delete bookings/refunds.

## EF Core & Database Configuration
**Configuration Pattern**: Each entity has an `IEntityTypeConfiguration<T>` in `BookingService.Infrastructure/Configurations/`:
```csharp
public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(32);
        // Relationships defined here with navigation properties
    }
}
```

**Automatic Registration**: Configurations are auto-discovered in `BookingDbContext.OnModelCreating()` via `ApplyConfigurationsFromAssembly()`.

**Patterns to Follow**:
- Enum properties: `HasConversion<string>()` + `HasMaxLength(32)`
- Monetary fields: `HasColumnType("decimal(18,2)")`
- String constraints: `HasMaxLength()` for all text columns
- Foreign keys explicitly mapped with `HasForeignKey()` and `OnDelete(DeleteBehavior.Restrict)`

## Core Entity Patterns
**Default Pattern**: All entities initialize collections with `= new List<T>()` and use `= default!` for required reference types:
```csharp
public ICollection<BookingItem> Items { get; set; } = new List<BookingItem>();
public User User { get; set; } = default!; // EF Core will populate at runtime
```

**GUIDs & Timestamps**:
- All primary keys are `Guid Id` (no auto-increment)
- Timestamp fields use `DateTimeOffset` (not `DateTime`) for timezone safety
- Booking tracks: `CreatedAt`, `ExpiresAt`, `ConfirmedAt?`, `CancelledAt?`

## Dependency Injection & Startup
`Program.cs` setup:
- `builder.Services.AddControllers()` registers controller discovery
- `app.MapControllers()` maps routes via controller attributes
- OpenAPI/Swagger mapped in Development only via `app.MapOpenApi()`
- HTTPS redirection enabled by default

**For Adding Services**: Register in `builder.Services` before `app.Build()`. Example:
```csharp
builder.Services.AddScoped<BookingDbContext>();
builder.Services.AddScoped<IBookingService, BookingService>();
```

## API Controllers
**Location**: `BookingService.Api/Controllers/`

**Controller Pattern**:
```csharp
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly BookingDbContext _context;
    
    public EventsController(BookingDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
    {
        // Implementation
    }
}
```

**Key Patterns**:
- **Route Convention**: `api/[controller]` produces `/api/events`, `/api/bookings`, etc.
- **Status Codes**: Use `Ok()`, `CreatedAtAction()`, `NoContent()`, `NotFound()`, `BadRequest()`
- **Async Methods**: All data operations use `async/await` pattern
- **DTOs as Records**: Request/response models are `record` types (immutable, nullsafe)
- **Constructor Injection**: DbContext injected via constructor, stored in read-only field

**Standard Endpoints by Controller**:

| Method | Route | Response | Purpose |
|--------|-------|----------|----------|
| GET | `/api/resource` | `Ok(resources)` | List all |
| GET | `/api/resource/{id}` | `Ok(resource)` or `NotFound()` | Get one |
| POST | `/api/resource` | `CreatedAtAction()` | Create |
| PUT | `/api/resource/{id}` | `NoContent()` or `BadRequest()` | Update/state change |
| DELETE | `/api/resource/{id}` | `NoContent()` or `NotFound()` | Delete |

**Examples in Codebase**:
- [EventsController](BookingService.Api/Controllers/EventsController.cs): Full CRUD + create/update DTOs
- [BookingsController](BookingService.Api/Controllers/BookingsController.cs): State transitions (`/confirm`, `/cancel` endpoints)
- [UsersController](BookingService.Api/Controllers/UsersController.cs): User management
- [TicketTypesController](BookingService.Api/Controllers/TicketTypesController.cs): Ticket type management

## Testing Conventions
Uses **xUnit** with implicit using:
```csharp
namespace BookingService.Tests;

public class BookingServiceTests
{
    [Fact]
    public void ShouldConfirmBooking() { }
    
    [Theory]
    [InlineData(BookingStatus.Pending)]
    public void ShouldHandleStatus(BookingStatus status) { }
}
```

**Test Project Scope**: Currently empty scaffold. Add tests to `BookingService.Tests` for service logic, integration tests for EF operations.

## Code Conventions
- **Nullable Reference Types**: Enabled (`<Nullable>enable</Nullable>`)—use `?` for optional properties, `!` suppression for EF-managed defaults
- **Implicit Usings**: Enabled—no manual `using System;` needed
- **Namespace Structure**: `BookingService.[Project].[Feature]` (e.g., `BookingService.Core.Entities`, `BookingService.Infrastructure.Configurations`)
- **Property Initialization**: Prefer initializer assignment (`= new List<T>()`) over empty constructors
- **Async Pattern**: Use `async/await` for database operations (not yet implemented—add where needed)

## Building & Running
**Build**: `dotnet build BookingService.sln`
**Run API**: `dotnet run --project BookingService.Api`
**Run Tests**: `dotnet test BookingService.Tests`

The API runs on HTTPS by default (configured in launchSettings.json).

## Common Development Workflows

### Adding a New Feature (e.g., Payment Processing)
1. **Core Layer**: Add `Payment` entity in `BookingService.Core/Entities/`
2. **Enum**: Add `PaymentStatus.cs` in `BookingService.Core/Enums/`
3. **Infrastructure**: Create `PaymentConfiguration.cs` with EF mappings
4. **Add Relationship**: Update `Booking` entity to include `ICollection<Payment> Payments`
5. **DbContext**: EF auto-discovers new config via `ApplyConfigurationsFromAssembly()`
6. **API**: Create `PaymentsController` in `BookingService.Api/Controllers/` with:
   - Standard CRUD endpoints (`[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`)
   - Request/response DTOs as `record` types
   - Proper HTTP status codes
7. **Tests**: Add test cases to `BookingService.Tests`

### Modifying Entity Relationships
- Always update both sides (navigation properties) for bidirectional relationships
- Use `HasForeignKey()` in configuration to explicitly map foreign key properties
- Remember: `OnDelete(DeleteBehavior.Restrict)` prevents cascading deletes

### Database Changes
- EF Core migrations are scaffolded but not yet committed
- After entity changes, run: `dotnet ef migrations add [MigrationName] -p BookingService.Infrastructure`
- Then: `dotnet ef database update -p BookingService.Infrastructure`

## Anti-Patterns to Avoid
- ❌ Cascade deletes on Bookings/Refunds (breaks data integrity)
- ❌ DateTime instead of DateTimeOffset (loses timezone info)
- ❌ Hardcoding enum strings without EF conversion configuration
- ❌ Optional reference types without `?` annotation
- ❌ Circular project references (breaks layering)

## File Location Quick Reference
- **Entities**: `BookingService.Core/Entities/` (User, Event, Booking, etc.)
- **Enums**: `BookingService.Core/Enums/` (BookingStatus, EventStatus, etc.)
- **EF Configs**: `BookingService.Infrastructure/Configurations/`
- **DbContext**: `BookingService.Infrastructure/Data/BookingDbContext.cs`
- **Controllers/Endpoints**: `BookingService.Api/` (controllers directory not yet visible but inferred)
- **Tests**: `BookingService.Tests/`
