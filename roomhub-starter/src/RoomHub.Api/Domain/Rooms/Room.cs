using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Domain.Rooms;

public sealed class Room
{
    private Room() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public int Capacity { get; private set; }

    // R1: a room has a non-empty name and a strictly positive capacity.
    public static Result<Room> Create(string? name, int capacity)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Room>.Failure(RoomErrors.NameRequired);

        if (capacity <= 0)
            return Result<Room>.Failure(RoomErrors.InvalidCapacity);

        return Result<Room>.Success(new Room
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Capacity = capacity
        });
    }
}
