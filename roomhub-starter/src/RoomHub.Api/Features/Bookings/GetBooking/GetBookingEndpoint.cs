using RoomHub.Api.Common.Http;
using RoomHub.Api.Domain.Bookings;

namespace RoomHub.Api.Features.Bookings.GetBooking;

public sealed record BookingResponse(
    Guid Id,
    Guid RoomId,
    string BookedBy,
    DateTime StartUtc,
    DateTime EndUtc,
    int Attendees,
    string Status);

public static class GetBookingEndpoint
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:guid}", Handle).WithName("GetBooking");

    private static async Task<IResult> Handle(
        Guid id,
        IBookingRepository bookingRepository,
        CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(id, cancellationToken);
        if (booking is null)
            return BookingErrors.NotFound(id).ToProblem();

        return Results.Ok(new BookingResponse(
            booking.Id,
            booking.RoomId,
            booking.BookedBy,
            booking.StartUtc,
            booking.EndUtc,
            booking.Attendees,
            booking.Status.ToString()));
    }
}
