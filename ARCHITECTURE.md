# BookingService - Architecture & Technical Decisions

This document explains the architectural patterns, design decisions, and technical choices made in the BookingService project.

## Solution Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Aspire AppHost                                  │
│                       (Orchestration & Discovery)                            │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                    ┌─────────────────┴─────────────────┐
                    ▼                                   ▼
        ┌───────────────────┐               ┌─────────────────────────────────┐
        │   SQL Server      │               │       BookingService.Api        │
        │   Container       │◄──────────────│  ┌───────────────────────────┐  │
        └───────────────────┘               │  │  BackgroundService        │  │
                                            │  │  (BookingExpiryService)   │  │
                                            │  └───────────────────────────┘  │
                                            └─────────────────────────────────┘
                                                          │
                              ┌────────────────────────────┼────────────────────────────┐
                              ▼                            ▼                            ▼
                    ┌─────────────────┐          ┌─────────────────┐          ┌─────────────────┐
                    │   Application   │          │  Infrastructure │          │     Worker      │
                    │   (Services,    │          │   (EF Core,     │          │  (Class Lib,    │
                    │    DTOs)        │          │    DbContext)   │          │   Background)   │
                    └─────────────────┘          └─────────────────┘          └─────────────────┘
                              │                            │
                              └──────────────┬─────────────┘
                                             ▼
                                   ┌─────────────────┐
                                   │      Core       │
                                   │   (Entities,    │
                                   │    Enums)       │
                                   └─────────────────┘
```

**Note**: The Worker project is a **class library** (not a separate executable). The `BookingExpiryService` 
background task runs **inside the API process** as a hosted service. This is a single deployable unit.

## Layer Responsibilities

| Layer | Project | Responsibility |
|-------|---------|----------------|
| **Presentation** | BookingService.Api | HTTP endpoints, authentication, request/response handling |
| **Application** | BookingService.Application | Business logic, service interfaces, DTOs |
| **Domain** | BookingService.Core | Entities, enums, domain rules, options classes |
| **Infrastructure** | BookingService.Infrastructure | Data access, EF Core DbContext, migrations |
| **Worker** | BookingService.Worker | Background tasks (class library, runs inside API) |
| **Orchestration** | BookingService.AppHost | Aspire host, container management, service wiring |

## Key Architectural Decisions

### 1. Clean Architecture with Vertical Slices

**Decision**: Organize code by feature within a layered architecture.

**Rationale**:
- Clear separation of concerns between layers
- Domain entities are independent of infrastructure
- Easy to test business logic in isolation
- Services are grouped by domain concept (Bookings, Events, Users)

**Trade-offs**:
- More projects to manage
- Some boilerplate for simple CRUD operations

### 2. Thin Controllers, Rich Services

**Decision**: Controllers only handle HTTP concerns; all business logic lives in Application services.

**Rationale**:
- Controllers become simple request/response mappers
- Business logic is reusable and testable
- Single Responsibility Principle adherence

**Example**:
```csharp
// Controller - thin, only HTTP concerns
[HttpPost("{id}/cancel")]
public async Task<ActionResult<BookingDto>> Cancel(Guid id)
{
    var booking = await _bookingsService.Cancel(id, userId);
    return booking == null ? NotFound() : Ok(booking);
}

// Service - contains business logic
public async Task<BookingDto?> Cancel(Guid bookingId, Guid userId)
{
    var booking = await _context.Bookings.FindAsync(bookingId);
    var result = _policyService.EvaluateCancellation(booking, evt);
    // ... business logic
}
```

### 3. Strategy Pattern for Policy Decisions

**Decision**: Separate `BookingPolicyService` from `BookingsService`.

**Rationale**:
- Cancellation/refund rules are complex and change independently
- Policy logic is isolated and easily testable
- Follows Single Responsibility Principle
- Enables future extension (different policies per event type)

**Structure**:
```
BookingsService          →  Orchestrates booking operations
BookingPolicyService     →  Evaluates business rules (refund eligibility, etc.)
```

### 4. Time Abstraction via ITimeProvider

**Decision**: Inject `ITimeProvider` instead of using `DateTimeOffset.UtcNow` directly.

**Rationale**:
- Enables deterministic unit testing
- Time-dependent logic can be tested without waiting
- Consistent time source across the application

**Implementation**:
```csharp
public interface ITimeProvider
{
    DateTimeOffset UtcNow { get; }
}

// Production: SystemTimeProvider returns real time
// Tests: Mock returns controlled time values
```

### 5. Configuration via IOptions Pattern

**Decision**: Use strongly-typed options classes bound from configuration.

**Rationale**:
- Type-safe configuration access
- Validation at startup
- Easy to mock in tests
- Centralized in `BookingOptions` class

**Consolidated Options**:
```csharp
public class BookingOptions
{
    public int TimeoutMinutes { get; set; } = 15;
    public int RefundCutoffHours { get; set; } = 24;
    public int ExpiryPollIntervalMinutes { get; set; } = 1;
}
```

### 6. DTOs for API Boundaries

**Decision**: Never expose domain entities directly through API endpoints.

**Rationale**:
- Decouples internal model from API contract
- Prevents over-posting attacks
- Allows API to evolve independently of domain
- Controls exactly what data is serialized

**Pattern**:
```
Entity (Core)  →  Service  →  DTO (Application)  →  Controller  →  HTTP Response
```

### 7. Optimistic Concurrency with RowVersion

**Decision**: Use SQL Server `rowversion` for concurrency control.

**Rationale**:
- Prevents lost updates in concurrent scenarios
- Automatic conflict detection by EF Core
- No explicit locking required

**Implementation**:
```csharp
public class Booking
{
    public byte[] RowVersion { get; private set; }
}

// EF Core configuration
entity.Property(e => e.RowVersion).IsRowVersion();
```

## Infrastructure Decisions

### 8. .NET Aspire for Orchestration

**Decision**: Use Aspire for local development and container orchestration.

**Rationale**:
- Simplified local development setup
- Automatic SQL Server container management
- Built-in observability (OpenTelemetry)
- Service discovery for microservices readiness
- Health checks out of the box

**Benefits**:
- One command to start entire stack
- Consistent environment across team
- Dashboard for monitoring and debugging

### 9. Secrets via Environment Variables

**Decision**: No secrets in configuration files; use environment variables and Aspire parameters.

**Rationale**:
- Secrets never committed to source control
- Works consistently across environments
- Aspire parameters provide secure injection
- Compatible with container orchestration (K8s, Docker)

**Hierarchy**:
```
Environment Variables  →  Aspire Parameters  →  User Secrets (dev only)
```

### 10. Background Service for Booking Expiry

**Decision**: Use `IHostedService` running in the API process.

**Rationale**:
- Simple deployment (single process)
- Shares DbContext and services
- Appropriate for current scale
- Can be extracted to separate worker if needed

**Trade-offs**:
- Runs in same process as API (resource sharing)
- For high scale, consider separate worker service or message queue

### 11. JWT Bearer Authentication

**Decision**: Stateless JWT tokens for API authentication.

**Rationale**:
- Stateless - no session storage required
- Self-contained claims for authorization
- Standard approach for REST APIs
- Works well with microservices

**Security Considerations**:
- Short token lifetime (60 minutes default)
- Signing key injected via environment variable
- HTTPS enforced in production

### 12. Central Package Management

**Decision**: Use `Directory.Packages.props` for NuGet version management.

**Rationale**:
- Single source of truth for package versions
- Prevents version drift across projects
- Easier security updates
- Cleaner `.csproj` files

## Testing Strategy

### Unit Tests
- Test services in isolation with mocked dependencies
- Use `ITimeProvider` mock for time-dependent tests
- Use `IOptions.Create()` for configuration

### Integration Tests
- Use `WebApplicationFactory` for API tests
- In-memory database for fast execution
- Test full request/response cycle

## Future Considerations

### Scalability Path
1. **Current**: Single API process with background worker
2. **Next**: Separate worker service with message queue
3. **Future**: Event-driven architecture with multiple services

### Potential Enhancements
- Redis for distributed caching
- Message queue for async operations
- Event sourcing for booking history
- API versioning for backward compatibility
- Rate limiting for API protection

## Summary

The architecture prioritizes:
- **Maintainability**: Clear separation of concerns, testable components
- **Security**: No hardcoded secrets, JWT authentication
- **Developer Experience**: Aspire for easy setup, comprehensive documentation
- **Flexibility**: Abstractions allow future changes without major rewrites
