# BookingService - Getting Started Guide

Event ticket booking system built with ASP.NET Core 9.0, Entity Framework Core, and .NET Aspire.

## Prerequisites

### Required
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for containerized deployment)

### Optional
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (17.9+) or [VS Code](https://code.visualstudio.com/)
- [Azure Data Studio](https://azure.microsoft.com/products/data-studio/) (for database management)

## Quick Start

### Option 1: Run with Aspire (Recommended)

This is the easiest way to run the application. Aspire will automatically start a SQL Server container.

```powershell
# 1. Clone and navigate to the project
cd BookingService

# 2. Run the setup script (installs Aspire workload if needed)
.\setup.ps1

# 3. Set the JWT secret (required - Aspire will prompt if not set)
$env:Parameters__jwt_key = "YourSecretKey_MinimumLength32Characters!"

# 4. Start the application
cd BookingService.AppHost
dotnet run
```

The Aspire Dashboard will open automatically at `https://localhost:17178`. From there you can:
- View the API endpoint URL
- Monitor logs, traces, and metrics
- Access the SQL Server container

**Default test users** (seeded automatically in Development):
| Email | Password | Role |
|-------|----------|------|
| admin@bookingservice.com | Password123! | Admin |
| organizer1@bookingservice.com | Password123! | Organizer |
| organizer2@bookingservice.com | Password123! | Organizer |
| customer1@bookingservice.com | Password123! | Customer |
| customer2@bookingservice.com | Password123! | Customer |

### Option 2: Run without Docker (LocalDB)

Use this if you don't have Docker or prefer SQL Server LocalDB.

```powershell
# 1. Navigate to API project
cd BookingService.Api

# 2. Set the JWT secret (required)
$env:Jwt__Key = "YourSecretKey_MinimumLength32Characters!"

# 3. Run the application
dotnet run
```

The API will be available at:
- Swagger UI: https://localhost:7292/swagger
- HTTP: http://localhost:5065

**Note:** LocalDB connection string is pre-configured in `appsettings.json`.

## Configuration

### Secrets Management

The JWT signing key is **not** stored in configuration files for security. Configure it using environment variables:

#### When running with Aspire (AppHost)
```powershell
# Set before running dotnet run in AppHost folder
$env:Parameters__jwt_key = "YourSecretKey_MinimumLength32Characters!"
```

If not set, Aspire will prompt for the secret value when starting.

#### When running standalone (without Aspire)
```powershell
# PowerShell - set before running dotnet run in Api folder
$env:Jwt__Key = "YourSecretKey_MinimumLength32Characters!"

# Or use .NET User Secrets (persists across sessions)
cd BookingService.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "YourSecretKey_MinimumLength32Characters!"
```

### Application Settings

Other configuration options in `BookingService.Api/appsettings.json`:

| Setting | Default | Description |
|---------|---------|-------------|
| `Booking:TimeoutMinutes` | 15 | Time before pending bookings expire |
| `Booking:RefundCutoffHours` | 24 | Hours before event when refunds are no longer allowed |
| `Booking:ExpiryPollIntervalMinutes` | 1 | How often to check for expired bookings |
| `Jwt:Issuer` | BookingService | JWT token issuer |
| `Jwt:Audience` | BookingServiceClients | JWT token audience |
| `Jwt:ExpiresInMinutes` | 60 | JWT token lifetime |

## Project Structure

```
BookingService/
├── BookingService.AppHost/        # Aspire orchestration host
├── BookingService.ServiceDefaults/# Shared Aspire configuration
├── BookingService.Api/            # ASP.NET Core Web API
├── BookingService.Application/    # Business logic and services
├── BookingService.Core/           # Domain entities and interfaces
├── BookingService.Infrastructure/ # Data access (EF Core)
├── BookingService.Worker/         # Background services
└── BookingService.Tests/          # Unit and integration tests
```

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login and get JWT token

### Events (Public)
- `GET /api/events` - List published events
- `GET /api/events/{id}` - Get event details

### Bookings (Authenticated)
- `POST /api/bookings` - Create booking
- `GET /api/bookings/{id}` - Get booking details
- `POST /api/bookings/{id}/confirm` - Confirm booking
- `POST /api/bookings/{id}/cancel` - Cancel booking

### Organizer Events (Organizer role)
- `GET /api/organizer/events` - List own events
- `POST /api/organizer/events` - Create event
- `PUT /api/organizer/events/{id}` - Update event
- `POST /api/organizer/events/{id}/publish` - Publish event

### Users (Admin role)
- `GET /api/users` - List all users
- `GET /api/users/{id}` - Get user details
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

## Development

### Running Tests

```powershell
cd BookingService
dotnet test
```

### Database Migrations

Migrations are applied automatically on startup. To create a new migration:

```powershell
cd BookingService
dotnet ef migrations add MigrationName --project BookingService.Infrastructure --startup-project BookingService.Api
```

### Health Checks

When running with Aspire, health endpoints are available:
- `/health` - Overall health status
- `/alive` - Liveness probe

## Deployment

### Docker Compose (Production)

Create a `docker-compose.yml`:

```yaml
version: '3.8'
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong!Password
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql

  api:
    build:
      context: .
      dockerfile: BookingService.Api/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__BookingDb=Server=sqlserver;Database=BookingServiceDb;User=sa;Password=YourStrong!Password;TrustServerCertificate=True
      - Jwt__Key=${JWT_KEY}
    ports:
      - "8080:8080"
    depends_on:
      - sqlserver

volumes:
  sqldata:
```

Run with:
```bash
JWT_KEY="YourProductionSecretKey_MinLength32!" docker-compose up -d
```

### Environment Variables for Production

| Variable | Required | Description |
|----------|----------|-------------|
| `ConnectionStrings__BookingDb` | Yes | SQL Server connection string |
| `Jwt__Key` | Yes | JWT signing key (min 32 characters) |
| `ASPNETCORE_ENVIRONMENT` | No | Set to `Production` |

## Troubleshooting

### "JWT Key not configured" error
- With Aspire: Set `$env:Parameters__jwt_key` before running AppHost
- Without Aspire: Set `$env:Jwt__Key` or use `dotnet user-secrets`

### Docker containers not starting
Ensure Docker Desktop is running and has sufficient resources allocated.

### Database connection failed
- With Aspire: Check the Aspire Dashboard for SQL Server container status
- Without Aspire: Ensure LocalDB is installed (`sqllocaldb info`)

### Port already in use
Change ports in `Properties/launchSettings.json` or stop conflicting services.
