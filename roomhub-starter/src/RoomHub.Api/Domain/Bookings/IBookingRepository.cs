namespace RoomHub.Api.Domain.Bookings;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetActiveForRoomAsync(Guid roomId, CancellationToken cancellationToken = default);

    // R9: how many active bookings a participant already has on a given UTC day (across all rooms).
    Task<int> CountActiveForBookedByOnDateAsync(string bookedBy, DateOnly dateUtc, CancellationToken cancellationToken = default);

    Task AddAsync(Booking booking, CancellationToken cancellationToken = default);
}
