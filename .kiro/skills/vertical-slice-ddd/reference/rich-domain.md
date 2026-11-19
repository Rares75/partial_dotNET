# Rich domain model (`Domain/<Aggregate>/`)

Entities are `sealed`, encapsulated, and self-validating. They are never constructed with a public constructor and never expose public setters. All creation goes through a static factory that returns `Result<T>`; all state changes go through behavior methods.

Replace `CampusHub`/`Event` with the target namespace and aggregate.

## The encapsulation contract
- `sealed class`.
- **Private parameterless constructor** — EF Core materializes through it.
- Every property is `{ get; private set; }`.
- No public constructor.
- Construction: `public static Result<T> Create(...)` validates inputs and returns `Result<T>.Failure(...)` or `Result<T>.Success(new T { ... })`.
- Behavior: methods that change state return `Result<Unit>` (or `Result<T>`), enforcing invariants/state machines and returning a domain error on violation.
- Child collections are private backing fields exposed only as `IReadOnlyCollection<>`.

## Entity template (`Event.cs`)
```csharp
using CampusHub.Domain.Common;
using CampusHub.Domain.Registrations;

namespace CampusHub.Domain.Events;

public sealed class Event
{
    private readonly List<Registration> _registrations = [];

    private Event() { }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateRange DateRange { get; private set; } = null!;
    public EventType Type { get; private set; }
    public EventStatus Status { get; private set; }
    public int Capacity { get; private set; }
    public int Version { get; private set; }

    public IReadOnlyCollection<Registration> Registrations => _registrations.AsReadOnly();

    public static Result<Event> Create(
        string title, string? description, DateRange dateRange, EventType type, int capacity)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result<Event>.Failure(EventErrors.TitleRequired);

        if (capacity <= 0)
            return Result<Event>.Failure(Error.Validation("Event.InvalidCapacity", "Capacity must be greater than zero."));

        return Result<Event>.Success(new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            DateRange = dateRange,
            Type = type,
            Status = EventStatus.Draft,
            Capacity = capacity,
            Version = 1
        });
    }

    // Behavior returning Result — enforces a state machine, never throws for a business rule.
    public Result<Unit> Publish(DateTime date)
    {
        if (Status == EventStatus.Cancelled)
            return Result<Unit>.Failure(EventErrors.EventAlreadyCancelled);

        if (Status == EventStatus.Published)
            return Result<Unit>.Failure(EventErrors.EventAlreadyPublished);

        Status = EventStatus.Published;
        return Result<Unit>.Success(Unit.Value);
    }
}
```

## Value object template (`DateRange.cs`)
A `sealed record` with a private constructor and a validating `Create` returning `Result`. Mapped inline via EF `ComplexProperty` (see `efcore-persistence.md`).
```csharp
using CampusHub.Domain.Common;

namespace CampusHub.Domain.Events;

public sealed record DateRange
{
    private DateRange() { }

    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }

    public static Result<DateRange> Create(DateTime startDate, DateTime endDate)
    {
        var startUtc = startDate.ToUniversalTime();
        var endUtc = endDate.ToUniversalTime();

        if (startUtc >= endUtc)
            return Result<DateRange>.Failure(Error.Validation("InvalidDateRange", "Start date must be before end date."));

        return Result<DateRange>.Success(new DateRange { StartDate = startUtc, EndDate = endUtc });
    }
}
```

## Child entity with `internal` factory (`Registration.cs`)
When an entity is created/mutated only from within the domain or by handlers in the same assembly, make the factory and state transitions `internal`. If the creation cannot fail, it may return the entity directly instead of `Result`.
```csharp
namespace CampusHub.Domain.Registrations;

public sealed class Registration
{
    private Registration() { }

    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid ParticipantId { get; private set; }
    public RegistrationStatus Status { get; private set; }
    public DateTime RegisteredAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    internal static Registration Create(Guid eventId, Guid participantId)
        => new()
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            ParticipantId = participantId,
            Status = RegistrationStatus.Confirmed,
            RegisteredAtUtc = DateTime.UtcNow
        };

    internal void Cancel()
    {
        Status = RegistrationStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
    }

    internal void PromoteToConfirmed()
    {
        Status = RegistrationStatus.Confirmed;
        CancelledAtUtc = null;
    }
}
```

## Enums
One enum file per concept, e.g.:
```csharp
namespace CampusHub.Domain.Events;
public enum EventStatus { Draft, Published, Cancelled }
```
```csharp
namespace CampusHub.Domain.Registrations;
public enum RegistrationStatus { Confirmed, Cancelled, Waitlisted }
```
EF persists these as strings (see configuration reference).

## Centralized domain errors (`<Aggregate>Errors.cs`)
Keep named errors in one place so codes/messages stay consistent and reusable.
```csharp
using CampusHub.Domain.Common;

namespace CampusHub.Domain.Events;

public static class EventErrors
{
    public static readonly Error TitleRequired =
        Error.Validation("Event.TitleRequired", "The event title is required.");
    public static readonly Error TitleTooLong =
        Error.Validation("Event.TitleTooLong", "The event title is too long.");
    public static readonly Error EventAlreadyPublished =
        Error.Conflict("Event.AlreadyPublished", "The event has already been published.");
    public static readonly Error EventAlreadyCancelled =
        Error.Conflict("Event.AlreadyCancelled", "The event has already been cancelled.");
}
```
**Use these fields in the factory/behavior methods** rather than inlining one-off `Error.Validation(...)` calls — that was an inconsistency worth fixing when reproducing the pattern. And guard `null` before length checks.
