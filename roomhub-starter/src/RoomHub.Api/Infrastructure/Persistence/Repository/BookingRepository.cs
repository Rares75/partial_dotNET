using Microsoft.EntityFrameworkCore;
using RoomHub.Api.Domain.Bookings;

namespace RoomHub.Api.Infrastructure.Persistence.Repository;

public class BookingRepository(RoomHubDbContext dbContext) : IBookingRepository
{
    public Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Booking>> GetActiveForRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
        => await dbContext.Bookings
            .Where(b => b.RoomId == roomId && b.Status == BookingStatus.Active)
            .ToListAsync(cancellationToken);

    public Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        dbContext.Bookings.Add(booking);
        return Task.CompletedTask;
    }
}
