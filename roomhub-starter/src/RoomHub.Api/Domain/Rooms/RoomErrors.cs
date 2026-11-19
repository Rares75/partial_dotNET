using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Domain.Rooms;

public static class RoomErrors
{
    public static readonly Error NameRequired =
        Error.Validation("Room.NameRequired", "Room name is required.");

    public static readonly Error InvalidCapacity =
        Error.Validation("Room.InvalidCapacity", "Capacity must be strictly positive.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("Room.NotFound", $"The room {id} does not exist.");
}
