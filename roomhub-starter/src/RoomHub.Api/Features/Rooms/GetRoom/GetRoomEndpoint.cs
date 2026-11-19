using RoomHub.Api.Common.Http;
using RoomHub.Api.Domain.Rooms;
using RoomHub.Api.Features.Rooms.CreateRoom;

namespace RoomHub.Api.Features.Rooms.GetRoom;

public static class GetRoomEndpoint
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:guid}", Handle).WithName("GetRoom");

    private static async Task<IResult> Handle(
        Guid id,
        IRoomRepository roomRepository,
        CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdAsync(id, cancellationToken);
        if (room is null)
            return RoomErrors.NotFound(id).ToProblem();

        return Results.Ok(new RoomResponse(room.Id, room.Name, room.Capacity));
    }
}
