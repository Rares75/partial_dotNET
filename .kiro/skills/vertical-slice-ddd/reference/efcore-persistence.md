# EF Core persistence (`Infrastructure/Persistence/`)

The `DbContext` implements `IUnitOfWork` so the same change-tracker that repositories stage into is the one the handler commits. Entity mapping lives in per-entity `IEntityTypeConfiguration` classes, auto-applied from the assembly.

Use whatever EF Core provider the project already targets (SQLite, SqlServer, Npgsql, ...). Templates below show SQLite; only the provider call in `Program.cs` differs.

## `CampusHubDbContext.cs`
```csharp
using CampusHub.Domain.Common;
using CampusHub.Domain.Events;
using CampusHub.Domain.Participants;
using CampusHub.Domain.Registrations;
using Microsoft.EntityFrameworkCore;

namespace CampusHub.Infrastructure.Persistence;

public class CampusHubDbContext(DbContextOptions<CampusHubDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Participant> Participants => Set<Participant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CampusHubDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    // Explicit interface implementation delegates to EF's own SaveChangesAsync.
    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        await base.SaveChangesAsync(cancellationToken);
    }
}
```
Add one `DbSet<>` per aggregate root. `ApplyConfigurationsFromAssembly` picks up every `IEntityTypeConfiguration` automatically, so you never edit `OnModelCreating` again.

## Entity configuration template (`Configurations/EventConfiguration.cs`)
```csharp
using CampusHub.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusHub.Infrastructure.Persistence.Configurations;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(1000);

        // Persist enums as readable strings, not ints.
        builder.Property(e => e.Type).IsRequired().HasConversion<string>();
        builder.Property(e => e.Status).IsRequired().HasConversion<string>();

        // Value object stored inline (no separate table) via ComplexProperty.
        builder.ComplexProperty(e => e.DateRange, dr =>
        {
            dr.Property(d => d.StartDate).IsRequired().HasColumnName("StartDate");
            dr.Property(d => d.EndDate).IsRequired().HasColumnName("EndDate");
        });
    }
}
```

## Unique index + simple config (`Configurations/ParticipantConfiguration.cs`)
```csharp
using CampusHub.Domain.Participants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusHub.Infrastructure.Persistence.Configurations;

public sealed class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.ToTable("Participants");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.FullName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Email).IsRequired().HasMaxLength(100);
        builder.HasIndex(p => p.Email).IsUnique();
    }
}
```

## Relationships + composite unique index (`Configurations/RegistrationConfiguration.cs`)
```csharp
using CampusHub.Domain.Events;
using CampusHub.Domain.Participants;
using CampusHub.Domain.Registrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusHub.Infrastructure.Persistence.Configurations;

public class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        builder.ToTable("Registrations");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Status).IsRequired().HasConversion<string>();

        builder.HasOne<Event>().WithMany()
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Participant>().WithMany()
            .HasForeignKey(r => r.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Prevent double-registration of the same participant for the same event.
        builder.HasIndex(r => new { r.EventId, r.ParticipantId }).IsUnique();
        builder.HasIndex(r => r.ParticipantId);
    }
}
```

## Mapping conventions to carry over
- Enums → strings via `.HasConversion<string>()`.
- Value objects → `ComplexProperty` (inline columns), not owned tables, unless you need a separate table.
- Encapsulated child collections map fine because of the private backing field + private setters; EF uses the private constructor.
- Enforce real invariants at the DB level too (unique indexes, FK delete behavior).

## Schema creation / migrations
The source project used `db.Database.EnsureCreated()` at startup (no migrations) — acceptable for prototypes. For anything shipping, prefer EF migrations instead: add the `Microsoft.EntityFrameworkCore.Design` package and run `dotnet ef migrations add Initial` / `dotnet ef database update`, and drop the `EnsureCreated()` call. Decide with the user which approach fits.
