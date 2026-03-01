var builder = DistributedApplication.CreateBuilder(args);

// SQL Server container with persistent volume for data
var sqlServer = builder.AddSqlServer("sql")
    .WithDataVolume("booking-sqldata");

// Database resource that the API will connect to
var database = sqlServer.AddDatabase("BookingDb");

// JWT secret parameter (will prompt or read from environment variable)
var jwtKey = builder.AddParameter("jwt-key", secret: true);

// API project with database dependency and JWT secret
builder.AddProject<Projects.BookingService_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("Jwt__Key", jwtKey);

builder.Build().Run();
