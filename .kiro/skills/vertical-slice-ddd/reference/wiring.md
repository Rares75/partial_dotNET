# Composition root (`Program.cs`)

Minimal-API wiring that ties the stack together. The single most important line is the `IUnitOfWork` registration: it must resolve the **same scoped `DbContext`** that repositories use, so staging and committing share one change-tracker.

Replace `CampusHub` and the provider with the target project's values.

## Registration order
```csharp
using CampusHub.Common.CQRS;
using CampusHub.Domain.Common;
using CampusHub.Domain.Events;
using CampusHub.Domain.Participants;
using CampusHub.Domain.Registrations;
using CampusHub.Feature.Events.CreateEvent;
using CampusHub.Feature.Events.GetEventById;
using CampusHub.Feature.Events.PublishEvent;
using CampusHub.Feature.Registrations;
using CampusHub.Infrastructure.Persistence;
using CampusHub.Infrastructure.Persistence.Repository;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// 1. DbContext (use the project's existing provider).
builder.Services.AddDbContext<CampusHubDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("CampusHubDB")));

// 2. IUnitOfWork === the same scoped DbContext instance. Critical.
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CampusHubDbContext>());

// 3. One scoped registration per repository.
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IParticipantRepository, ParticipantRepository>();
builder.Services.AddScoped<IRegistrationRepository, RegistrationRepository>();

// 4. Reflection scan registers every ICommandHandler/IQueryHandler. No per-handler lines.
builder.Services.AddHandlers(typeof(Program).Assembly);

var app = builder.Build();

// Prototype schema creation — prefer EF migrations for real deployments.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CampusHubDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 5. Map each slice's endpoint under its aggregate's route group.
var events = app.MapGroup("/events");
CreateEventEndpoint.Map(events);
GetEventByIdEndpoint.Map(events);
PublishEventEndpoint.Map(events);

var registrations = app.MapGroup("/registrations");
RegisterForEventEndpoint.Map(registrations);

app.Run();
```

## Connection string (`appsettings.json`)
```json
{
  "ConnectionStrings": {
    "CampusHubDB": "Data Source=CampusHub.db"
  }
}
```

## `.csproj` packages
Add only what's missing; match the project's target framework and provider.
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="<match-tfm>" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="<match-tfm>" />
  <!-- For migrations instead of EnsureCreated: -->
  <!-- <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="<match-tfm>" /> -->
</ItemGroup>
```
`net`-current projects typically enable `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>` — the templates assume both.

## Wiring checklist when adding things later
- **New handler** → nothing to do; `AddHandlers` finds it.
- **New repository** → add one `AddScoped<IRepo, Repo>()` line.
- **New aggregate** → add a `DbSet<>` to the `DbContext` (its `IEntityTypeConfiguration` is auto-applied).
- **New slice** → add one `XyzEndpoint.Map(group)` line under the right `MapGroup`.

## Final verification
After wiring, run `dotnet build` and start the app. Exercise one slice end to end (e.g. create, then a failing validation case) to confirm the Result→HTTP mapping and the Unit-of-Work commit actually work. Files existing is not the bar; a passing request is.
