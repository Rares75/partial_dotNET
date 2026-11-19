using RoomHub.Api.Common.CQRS;
using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.Api.Features.Bookings.CreateBooking;

public sealed record CreateBookingCommand(
    Guid RoomId,
    string? BookedBy,
    DateTime StartUtc,
    DateTime EndUtc,
    int Attendees) : ICommand<Booking>;

public sealed class CreateBookingHandler(
    IRoomRepository roomRepository,
    IBookingRepository bookingRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateBookingCommand, Booking>
{
    public async Task<Result<Booking>> HandleAsync(CreateBookingCommand command, CancellationToken cancellationToken = default)
    {
        // The room must exist: 404 is returned before any validation.
        var room = await roomRepository.GetByIdAsync(command.RoomId, cancellationToken);
        if (room is null)
            return Result<Booking>.Failure(RoomErrors.NotFound(command.RoomId));

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var bookingResult = Booking.Create(
            room, command.BookedBy, command.StartUtc, command.EndUtc, command.Attendees, nowUtc);
        if (bookingResult.IsFailure)
            return Result<Booking>.Failure(bookingResult.Error);

        var booking = bookingResult.Value;

        // R7: no overlap with existing active bookings of the same room.
        var activeBookings = await bookingRepository.GetActiveForRoomAsync(command.RoomId, cancellationToken);
        foreach (var existing in activeBookings)
        {
            if (existing.OverlapsWith(booking.StartUtc, booking.EndUtc))
                return Result<Booking>.Failure(BookingErrors.RoomOccupied);
        }

        // R9: a participant may hold at most 2 active bookings per UTC day, across all rooms.
        var bookingDateUtc = DateOnly.FromDateTime(booking.StartUtc.ToUniversalTime());
        var activeThatDay = await bookingRepository.CountActiveForBookedByOnDateAsync(
            booking.BookedBy, bookingDateUtc, cancellationToken);
        if (activeThatDay >= 2)
            return Result<Booking>.Failure(BookingErrors.DailyLimitReached);

        await bookingRepository.AddAsync(booking, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Booking>.Success(booking);
    }
}
