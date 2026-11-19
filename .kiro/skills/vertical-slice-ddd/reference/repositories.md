# Repository interfaces + implementations

Interfaces live in the **domain** next to their aggregate (dependency inversion: infrastructure depends on the domain, never the reverse). Implementations live in `Infrastructure/Persistence/Repository/`.

The cardinal rule: **repositories only stage changes; they never call `SaveChanges`.** `Add*` methods call `DbSet.Add` and return `Task.CompletedTask`. The handler commits via `IUnitOfWork.SaveChangesAsync`.

## Interface (in `Domain/<Aggregate>/`)
```csharp
namespace CampusHub.Domain.Events;

public interface IEventRepository
{
    Task<Event?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddEventAsync(Event @event);

    Task<IReadOnlyList<Event>> GetActiveRangesForParticipantAsync(
        Guid participantId, DateTime nowUtc, CancellationToken cancellationToken = default);
}
```
Keep interfaces minimal — expose only the queries/commands the handlers actually need, not a generic CRUD surface. Reads return the entity (or `null`); stage-only writes return `Task`.

## Implementation (in `Infrastructure/Persistence/Repository/`)
Use a primary constructor taking the concrete `DbContext`.
```csharp
using CampusHub.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Infrastructure.Persistence.Repository;

public class EventRepository(CampusHubDbContext dbContext) : IEventRepository
{
    public Task<Event?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Events.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task AddEventAsync(Event @event)
    {
        dbContext.Events.Add(@event);   // stage only
        return Task.CompletedTask;      // no SaveChanges here
    }

    public async Task<IReadOnlyList<Event>> GetActiveRangesForParticipantAsync(
        Guid participantId, DateTime nowUtc, CancellationToken cancellationToken = default)
        => await dbContext.Events
            .Where(e => dbContext.Registrations.Any(r => r.ParticipantId == participantId && r.EventId == e.Id)
                        && e.DateRange.StartDate <= nowUtc && e.DateRange.EndDate >= nowUtc)
            .ToListAsync(cancellationToken);
}
```

## Minimal stage-only repository
```csharp
using CampusHub.Domain.Registrations;

namespace CampusHub.Infrastructure.Persistence.Repository;

public class RegistrationRepository(CampusHubDbContext dbContext) : IRegistrationRepository
{
    public Task AddRegistrationAsync(Registration registration, CancellationToken cancellationToken = default)
    {
        dbContext.Registrations.Add(registration);
        return Task.CompletedTask;
    }
}
```

Register each repository as scoped in `Program.cs` (see `wiring.md`).
