using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.Api.Domain.Bookings;

public sealed class Booking
{
    public static readonly TimeSpan MaxDurationStandard = TimeSpan.FromHours(4);
    public static readonly TimeSpan MaxDurationLargeRoom = TimeSpan.FromHours(2);
    public const int LargeRoomCapacityThreshold = 10;

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

        // R6: large rooms (capacity > 10) are capped at 2 hours; others keep the 4-hour limit.
        var maxDuration = room.Capacity > LargeRoomCapacityThreshold
            ? MaxDurationLargeRoom
            : MaxDurationStandard;
        if (endUtc - startUtc > maxDuration)
            return Result<Booking>.Failure(BookingErrors.TooLong(maxDuration));

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
    // A booking can only be cancelled at least 60 minutes before it starts.
    public static readonly TimeSpan MinCancellationLeadTime = TimeSpan.FromMinutes(60);

    public Result<Unit> Cancel(DateTime nowUtc)
    {
        if (Status == BookingStatus.Cancelled)
            return Result<Unit>.Failure(BookingErrors.AlreadyCancelled);

        if (StartUtc - nowUtc < MinCancellationLeadTime)
            return Result<Unit>.Failure(BookingErrors.CancellationTooLate);

        Status = BookingStatus.Cancelled;
        return Result<Unit>.Success(Unit.Value);
    }
}
