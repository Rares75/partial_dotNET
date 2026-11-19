---
name: vertical-slice-ddd
description: "Use when adding Vertical Slice Architecture, a rich DDD domain model, a Result/Error functional-error pattern, a library-free CQRS command/query mediator, and EF Core persistence with the Repository + Unit of Work pattern to an existing .NET web project that does not yet have them. Covers scaffolding the Common/CQRS abstractions, Domain/Common primitives (Result, Error, ErrorType, Unit, IUnitOfWork), rich aggregates (private ctor + static factory returning Result + Result-returning behavior), feature slices (co-located command/query + handler + minimal-API endpoint), EF Core DbContext that implements IUnitOfWork, IEntityTypeConfiguration classes, repository implementations, and Program.cs wiring. Not for frontend/UI, and not for projects that already use these patterns (extend them instead)."
version: 1.0.0
user-invocable: true
argument-hint: "[scaffold | add-slice <Aggregate> <UseCase> | add-aggregate <Name>] [target project path]"
license: Apache 2.0
---

This skill integrates five patterns, as one coherent stack, into an existing .NET web project that lacks them:

1. **Vertical Slice Architecture** — one folder per use case under `Feature/`, each holding an endpoint + handler (+ co-located command/query).
2. **Rich domain model (DDD)** — sealed entities with private constructors, private setters, static factory methods, and behavior methods that return `Result`. No anemic models, no public setters.
3. **Result / Error** — a functional error type so domain and handlers return failures instead of throwing.
4. **Library-free CQRS** — hand-rolled `ICommand`/`IQuery`/`ICommandHandler`/`IQueryHandler` with reflection-based DI registration. No MediatR.
5. **EF Core + Repository + Unit of Work** — a `DbContext` that implements `IUnitOfWork`, per-entity `IEntityTypeConfiguration`, and repositories that only stage changes while the Unit of Work commits.

The reference files carry the exact code templates. This file is the map and the rules.

## Before you touch anything

1. **Confirm the project does not already have these patterns.** Look for an existing `Common/CQRS`, `Domain/Common/Result.cs`, or an `IUnitOfWork`. If any exist, stop and ask whether to extend them rather than scaffolding duplicates. This skill is for projects *without* them.
2. **Read the target project's conventions first.** Find the root namespace, target framework, and the `Program.cs`/`Startup` style (minimal API vs. controllers). Match them. The templates below assume minimal APIs, `net`-current, `Nullable` and `ImplicitUsings` enabled, and namespaces that mirror folder paths. Adapt the templates to the project rather than imposing a new root namespace.
3. **Check NuGet packages.** The persistence layer needs an EF Core provider (`Microsoft.EntityFrameworkCore.<Provider>`). Add it only if absent; use the provider the project already targets (SQLite, SqlServer, Npgsql, etc.) — do not switch providers.
4. **Decide scope with the user.** Scaffolding the base abstractions is step one. Adding a concrete aggregate or slice is separate. Agree on which aggregates/use cases to build before generating files.

## Workflow

### Mode: scaffold (first-time setup)
Create the cross-cutting foundation, in this order. Each item maps to a reference file.

1. `Domain/Common/` primitives — [reference/result-error.md](reference/result-error.md): `Error.cs`, `ErrorType.cs`, `Result.cs`, `Unit.cs`, `IUnitOfWork.cs`.
2. `Common/CQRS/` abstractions — [reference/cqrs.md](reference/cqrs.md): `ICommand.cs`, `IQuery.cs`, `ICommandHandler.cs`, `IQueryHandler.cs`, `HandlerRegistrationExtensions.cs`.
3. `Infrastructure/Persistence/` — [reference/efcore-persistence.md](reference/efcore-persistence.md): the `DbContext` implementing `IUnitOfWork` (do not create it empty — it needs at least one `DbSet` to compile cleanly; create it together with the first aggregate).
4. `Program.cs` wiring — [reference/wiring.md](reference/wiring.md): register `DbContext`, map `IUnitOfWork` to the same scoped `DbContext` instance, register repositories, call `AddHandlers(typeof(Program).Assembly)`, and map endpoint groups.

Then build at least one aggregate + one slice so the project compiles and runs.

### Mode: add-aggregate
For a new aggregate (e.g. `Order`, `Customer`):
1. `Domain/<Aggregate>/` — the entity, its enums, a value object if needed, the `<Aggregate>Errors` static class, and the repository interface. See [reference/rich-domain.md](reference/rich-domain.md) and [reference/repositories.md](reference/repositories.md).
2. `Infrastructure/Persistence/Configurations/<Aggregate>Configuration.cs` and `Repository/<Aggregate>Repository.cs` — [reference/efcore-persistence.md](reference/efcore-persistence.md) and [reference/repositories.md](reference/repositories.md).
3. Add the `DbSet<>` to the `DbContext` and register the repository in `Program.cs`.

### Mode: add-slice
For a new use case on an existing aggregate (e.g. `CancelOrder`):
1. Create `Feature/<Aggregate>/<UseCase>/` with `<UseCase>Handler.cs` (command/query co-located at the top of the file) and `<UseCase>Endpoint.cs`. See [reference/feature-slice.md](reference/feature-slice.md).
2. Map the endpoint in `Program.cs` under the aggregate's route group.
3. The handler is discovered automatically by `AddHandlers` — no manual DI registration for handlers.

## Rules that keep the stack coherent

- **Commands return `Result<T>`; queries return raw `T`.** `ICommandHandler` returns `Task<Result<TResult>>`, `IQueryHandler` returns `Task<TResult>`. This asymmetry is deliberate: commands can fail with domain errors, reads are treated as non-failing. Keep it.
- **Domain never throws for expected failures.** Validation and business-rule violations flow as `Result.Failure(Error.X(...))`. Reserve exceptions for truly exceptional/programmer errors.
- **Entities are constructed only through static factories.** Private parameterless constructor (for EF), private setters, and a `static Result<T> Create(...)` that validates. Behavior methods mutate state and return `Result`.
- **Repositories stage; the Unit of Work commits.** `Add*` methods call `DbSet.Add` and return `Task.CompletedTask`. Persistence happens only in `IUnitOfWork.SaveChangesAsync`, called by the handler. Never call `SaveChanges` inside a repository.
- **The `DbContext` *is* the `IUnitOfWork`.** It implements the interface, and `Program.cs` registers `IUnitOfWork` as a factory that resolves the same scoped `DbContext`, so repositories and the UoW share one change-tracker.
- **Namespaces mirror folders**, repository interfaces live in `Domain/` next to their aggregate (dependency inversion), and endpoints translate `Error.Type` to HTTP status codes (`Validation → 400`, `NotFound → 404`, `Conflict → 409`).
- **Verify it compiles and runs.** After scaffolding, build the project and start it. A clean `dotnet build` plus a successful request through one slice is the completion bar — not just "files created".

## Known pitfalls to avoid (these appeared in the source project)

- Don't let a value-object or entity factory check `.Length` before a null check — guard null first.
- Keep error codes consistent (one convention, e.g. dotted `"Event.TitleRequired"`), and actually use the centralized `<Aggregate>Errors` fields instead of inlining ad-hoc codes.
- Don't skip the query handler on read endpoints unless you intend to; if a slice is a pure read, it's fine for the endpoint to use the repository directly, but be deliberate about it.
- Watch spelling in public identifiers (file names, constants) — they leak into the API surface and are painful to rename later.
