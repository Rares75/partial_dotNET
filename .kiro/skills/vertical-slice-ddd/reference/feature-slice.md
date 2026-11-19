# Feature slice (`Feature/<Aggregate>/<UseCase>/`)

One folder per use case. Two files:
- `<UseCase>Handler.cs` — the command/query record **co-located at the top of the file**, then the handler.
- `<UseCase>Endpoint.cs` — a `static class` with `Map(RouteGroupBuilder)` and a `private static async Task<IResult> Handle(...)`.

The endpoint injects the handler interface directly (DI/minimal-API binding resolves it). Failures are translated to HTTP by switching on `Error.Type`.

## Command slice — handler (`CreateEventHandler.cs`)
```csharp
using CampusHub.Common.CQRS;
using CampusHub.Domain.Common;
using CampusHub.Domain.Events;

namespace CampusHub.Feature.Events.CreateEvent;

public sealed record CreateEventCommand(
    string Title,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    EventType Type,
    int Capacity) : ICommand<Event>;

public sealed class CreateEventHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateEventCommand, Event>
{
    public async Task<Result<Event>> HandleAsync(CreateEventCommand command, CancellationToken cancellationToken = default)
    {
        var dateRangeResult = DateRange.Create(command.StartDate, command.EndDate);
        if (dateRangeResult.IsFailure)
            return Result<Event>.Failure(dateRangeResult.Error);

        var eventResult = Event.Create(
            command.Title, command.Description, dateRangeResult.Value, command.Type, command.Capacity);
        if (eventResult.IsFailure)
            return Result<Event>.Failure(eventResult.Error);

        await eventRepository.AddEventAsync(eventResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);   // handler commits, not the repo

        return Result<Event>.Success(eventResult.Value);
    }
}
```
Handler shape: `sealed record` command implementing `ICommand<T>`; `sealed class` handler using a **primary constructor** for DI; short-circuit on each `IsFailure`, orchestrate domain + repository, then `SaveChangesAsync`.

## Command slice — endpoint (`CreateEventEndpoint.cs`)
```csharp
using CampusHub.Common.CQRS;
using CampusHub.Domain.Events;

namespace CampusHub.Feature.Events.CreateEvent;

public static class CreateEventEndpoint
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", Handle).WithName("CreateEvent");

    private static async Task<IResult> Handle(
        CreateEventCommand command,
        ICommandHandler<CreateEventCommand, Event> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
            return Results.BadRequest(new { result.Error.Code, result.Error.Message });

        return Results.Created($"/events/{result.Value.Id}", result.Value);
    }
}
```

## Multi-repository orchestration + full error mapping (`RegisterForEventHandler.cs` / endpoint)
Handler coordinates several repositories, enforces a cross-aggregate rule, then commits once:
```csharp
using CampusHub.Common.CQRS;
using CampusHub.Domain.Common;
using CampusHub.Domain.Events;
using CampusHub.Domain.Participants;
using CampusHub.Domain.Registrations;

namespace CampusHub.Feature.Registrations;

public sealed record RegisterForEventCommand(Guid EventId, Guid ParticipantId) : ICommand<Registration>;

public sealed class RegisterForEventHandler(
    IEventRepository eventRepository,
    IParticipantRepository participantRepository,
    IRegistrationRepository registrationRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RegisterForEventCommand, Registration>
{
    public async Task<Result<Registration>> HandleAsync(RegisterForEventCommand command, CancellationToken cancellationToken = default)
    {
        var @event = await eventRepository.GetEventByIdAsync(command.EventId, cancellationToken);
        if (@event is null)
            return Result<Registration>.Failure(Error.NotFound("EventNotFound", $"Event with ID {command.EventId} not found."));

        var participant = await participantRepository.GetParticipantByIdAsync(command.ParticipantId);
        if (participant is null)
            return Result<Registration>.Failure(Error.NotFound("ParticipantNotFound", $"Participant with ID {command.ParticipantId} not found."));

        var activeRanges = await eventRepository.GetActiveRangesForParticipantAsync(command.ParticipantId, DateTime.UtcNow, cancellationToken);
        foreach (var range in activeRanges)
        {
            if (range.DateRange.StartDate < @event.DateRange.EndDate && @event.DateRange.StartDate < range.DateRange.EndDate)
                return Result<Registration>.Failure(Error.Validation("OverlappingEvent", "The participant is already registered for an overlapping event."));
        }

        var registration = Registration.Create(command.EventId, command.ParticipantId);
        await registrationRepository.AddRegistrationAsync(registration, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Registration>.Success(registration);
    }
}
```
Endpoint maps `NotFound → 404`, everything else → 400:
```csharp
private static async Task<IResult> Handle(
    RegisterForEventCommand command,
    ICommandHandler<RegisterForEventCommand, Registration> handler,
    CancellationToken cancellationToken)
{
    var result = await handler.HandleAsync(command, cancellationToken);

    if (result.IsFailure)
    {
        return result.Error.Type switch
        {
            ErrorType.NotFound => Results.NotFound(new { result.Error.Code, result.Error.Message }),
            _ => Results.BadRequest(new { result.Error.Code, result.Error.Message })
        };
    }

    return Results.Created($"/registrations/{result.Value.Id}", result.Value);
}
```

## `Unit` command + route parameter (`PublishEventEndpoint.cs`)
When the command carries no body, build it from the route parameter, and return `NoContent` on success. Map `Conflict → 409`:
```csharp
public static void Map(RouteGroupBuilder group) =>
    group.MapPost("/{id:guid}/publish", Handle).WithName("PublishEvent");

private static async Task<IResult> Handle(
    Guid id,
    ICommandHandler<PublishEventCommand, Unit> handler,
    CancellationToken cancellationToken)
{
    var result = await handler.HandleAsync(new PublishEventCommand(id), cancellationToken);

    if (result.IsFailure)
        return result.Error.Type == ErrorType.NotFound
            ? Results.NotFound(new { result.Error.Code, result.Error.Message })
            : Results.Conflict(new { result.Error.Code, result.Error.Message });

    return Results.NoContent();
}
```

## Query slice
Query record implements `IQuery<T>`; handler implements `IQueryHandler<TQuery, T>` and returns raw `T` (no `Result`). For a simple read the endpoint may inject the repository directly instead of going through a query handler — but be deliberate about that choice.

## Status-code mapping (consistent across endpoints)
| `ErrorType`  | HTTP result            |
|--------------|------------------------|
| `Validation` | `Results.BadRequest`   |
| `NotFound`   | `Results.NotFound`     |
| `Conflict`   | `Results.Conflict`     |
| success      | `Created` / `Ok` / `NoContent` |

Return the error body as `new { result.Error.Code, result.Error.Message }`.
