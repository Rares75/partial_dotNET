using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.Api.Domain.Bookings;

public sealed class Booking
{
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(4);

    private Booking() { }

    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public string BookedBy { get; private set; } = null!;
    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }
    public int Attendees { get; private set; }
    public BookingStatus Status { get; private set; }

    // Enforces R2, R3, R4, R5, R6. R7 (overlap) is cross-booking and lives in the handler.
    public static Result<Booking> Create(
        Room room,
        string? bookedBy,
        DateTime startUtc,
        DateTime endUtc,
        int attendees,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(bookedBy))
            return Result<Booking>.Failure(BookingErrors.BookedByRequired);

        if (endUtc <= startUtc)
            return Result<Booking>.Failure(BookingErrors.InvalidRange);

        if (attendees <= 0)
            return Result<Booking>.Failure(BookingErrors.InvalidAttendees);

        if (attendees > room.Capacity)
            return Result<Booking>.Failure(BookingErrors.AttendeesExceedCapacity);

        if (startUtc < nowUtc)
            return Result<Booking>.Failure(BookingErrors.StartsInThePast);

        if (endUtc - startUtc > MaxDuration)
            return Result<Booking>.Failure(BookingErrors.TooLong);

        return Result<Booking>.Success(new Booking
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            BookedBy = bookedBy.Trim(),
            StartUtc = startUtc,
            EndUtc = endUtc,
            Attendees = attendees,
            Status = BookingStatus.Active
        });
    }

    // R7: two active bookings overlap only when they truly intersect.
    // Touching intervals (one ends exactly when the other starts) do NOT overlap.
    public bool OverlapsWith(DateTime startUtc, DateTime endUtc)
        => Status == BookingStatus.Active && StartUtc < endUtc && startUtc < EndUtc;

    // R8: cancelling an active booking succeeds; cancelling an already cancelled one is a conflict.
    public Result<Unit> Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            return Result<Unit>.Failure(BookingErrors.AlreadyCancelled);

        Status = BookingStatus.Cancelled;
        return Result<Unit>.Success(Unit.Value);
    }
}
