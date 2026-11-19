using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Domain.Bookings;

public static class BookingErrors
{
    // R2
    public static readonly Error BookedByRequired =
        Error.Validation("Booking.BookedByRequired", "BookedBy is required.");

    // R3
    public static readonly Error InvalidRange =
        Error.Validation("Booking.InvalidRange", "End must be strictly after start.");

    // R4
    public static readonly Error InvalidAttendees =
        Error.Validation("Booking.InvalidAttendees", "Attendees must be strictly positive.");

    public static readonly Error AttendeesExceedCapacity =
        Error.Validation("Booking.AttendeesExceedCapacity", "Attendees exceed the room capacity.");

    // R5
    public static readonly Error StartsInThePast =
        Error.Validation("Booking.StartsInThePast", "A booking cannot start in the past.");

    // R6
    public static Error TooLong(TimeSpan maxDuration) =>
        Error.Validation("Booking.TooLong", $"The maximum booking duration is {maxDuration.TotalHours:0.#} hours.");

    // R7
    public static readonly Error RoomOccupied =
        Error.Conflict("Booking.RoomOccupied", "The room is occupied in this interval.");

    // R8
    public static readonly Error AlreadyCancelled =
        Error.Conflict("Booking.AlreadyCancelled", "The booking is already cancelled.");

    public static readonly Error CancellationTooLate =
        Error.Conflict("Booking.CancellationTooLate", "A booking can only be cancelled at least 60 minutes before it starts.");

    // R9
    public static readonly Error DailyLimitReached =
        Error.Conflict("Booking.DailyLimitReached", "A participant cannot have more than 2 active bookings per day.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("Booking.NotFound", $"The booking {id} does not exist.");
}
