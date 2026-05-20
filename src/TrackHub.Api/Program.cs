using Microsoft.EntityFrameworkCore;
using TrackHub.Application.Connectors;
using TrackHub.Application.Services;
using TrackHub.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TrackHubDbContext>(opt =>
    opt.UseOracle(builder.Configuration.GetConnectionString("Oracle")));

// Connector wiring
builder.Services.AddSingleton<ConnectorFactory>();

// Application services
builder.Services.AddSingleton<EventIngestionService>();
builder.Services.AddScoped<ShipmentReportService>();

builder.Services.AddControllers();
builder.Services.AddAuthentication("Jwt");
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
