using Microsoft.EntityFrameworkCore;
using RoomHub.Api.Common.CQRS;
using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;
using RoomHub.Api.Features.Bookings.CancelBooking;
using RoomHub.Api.Features.Bookings.CreateBooking;
using RoomHub.Api.Features.Bookings.GetBooking;
using RoomHub.Api.Features.Rooms.CreateRoom;
using RoomHub.Api.Features.Rooms.GetRoom;
using RoomHub.Api.Infrastructure.Persistence;
using RoomHub.Api.Infrastructure.Persistence.Repository;

var builder = WebApplication.CreateBuilder(args);

// EF Core + SQLite: data survives a restart.
builder.Services.AddDbContext<RoomHubDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("RoomHubDB")));

// IUnitOfWork is the same scoped DbContext instance repositories stage into.
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<RoomHubDbContext>());

builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();

// R5: "now" comes from TimeProvider, never DateTime.UtcNow.
builder.Services.AddSingleton(TimeProvider.System);

// Reflection scan registers every ICommandHandler/IQueryHandler.
builder.Services.AddHandlers(typeof(Program).Assembly);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RoomHubDbContext>();
    db.Database.EnsureCreated();
}

app.MapGet("/", () => "RoomHub API is running.");

var rooms = app.MapGroup("/api/rooms").WithTags("Rooms");
CreateRoomEndpoint.Map(rooms);
GetRoomEndpoint.Map(rooms);
CreateBookingEndpoint.Map(rooms);

var bookings = app.MapGroup("/api/bookings").WithTags("Bookings");
GetBookingEndpoint.Map(bookings);
CancelBookingEndpoint.Map(bookings);

app.Run();
