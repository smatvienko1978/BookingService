using System.Text;
using BookingService.Worker;
using BookingService.Application.Interfaces;
using BookingService.Application.Services;
using BookingService.Core.Entities;
using BookingService.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// EF Core DbContext with Aspire SQL Server integration
// When running via Aspire AppHost, connection string is injected automatically as "BookingDb"
// When running standalone, falls back to "DefaultConnection" from appsettings.json
builder.AddSqlServerDbContext<BookingDbContext>("BookingDb", configureSettings: settings =>
{
    settings.DisableRetry = false;
}, configureDbContextOptions: options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
});

// Options
builder.Services.Configure<BookingOptions>(builder.Configuration.GetSection("Booking"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// JWT Authentication
// Key is injected via environment variable (Jwt__Key) when running with Aspire
// For standalone development, set the environment variable or use user-secrets
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] 
    ?? throw new InvalidOperationException(
        "JWT Key not configured. Set 'Jwt__Key' environment variable or run via Aspire AppHost.");
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

// Background services
builder.Services.AddHostedService<BookingExpiryService>();

// Time provider
builder.Services.AddSingleton<ITimeProvider, SystemTimeProvider>();

// Application services
builder.Services.AddScoped<IBookingPolicyService, BookingPolicyService>();
builder.Services.AddScoped<IBookingsService, BookingsService>();
builder.Services.AddScoped<IEventsService, EventsService>();
builder.Services.AddScoped<IOrganizerEventsService, OrganizerEventsService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsersService, UsersService>();

// Database initializer
builder.Services.AddScoped<DatabaseInitializer>();

var app = builder.Build();

// Initialize database (apply migrations and seed data in development)
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync(app.Environment.IsDevelopment());
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map Aspire default endpoints (health checks)
app.MapDefaultEndpoints();

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }

