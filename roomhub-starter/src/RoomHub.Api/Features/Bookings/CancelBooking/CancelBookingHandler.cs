using RoomHub.Api.Common.CQRS;
using RoomHub.Api.Domain.Bookings;
using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Features.Bookings.CancelBooking;

public sealed record CancelBookingCommand(Guid BookingId) : ICommand<Unit>;

public sealed class CancelBookingHandler(
    IBookingRepository bookingRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CancelBookingCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(CancelBookingCommand command, CancellationToken cancellationToken = default)
    {
        var booking = await bookingRepository.GetByIdAsync(command.BookingId, cancellationToken);
        if (booking is null)
            return Result<Unit>.Failure(BookingErrors.NotFound(command.BookingId));

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var cancelResult = booking.Cancel(nowUtc);
        if (cancelResult.IsFailure)
            return Result<Unit>.Failure(cancelResult.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Unit>.Success(Unit.Value);
    }
}
