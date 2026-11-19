# Library-free CQRS (`Common/CQRS/`)

A hand-rolled command/query mediator — no MediatR, no pipeline. Handlers are registered by reflection and injected into endpoints directly; the minimal-API parameter binder resolves the concrete handler. There is no `ISender`/dispatcher.

Replace `CampusHub` with the target root namespace.

## The asymmetry (important)
- **Commands can fail** → `ICommandHandler` returns `Task<Result<TResult>>`.
- **Queries are non-failing reads** → `IQueryHandler` returns `Task<TResult>` (raw, not wrapped).

Keep this split. If a read genuinely needs to fail with a domain error, model it as a command or have the endpoint handle the null/empty case.

## `ICommand.cs`
```csharp
namespace CampusHub.Common.CQRS;

public interface ICommand<TResult>
{
}
```

## `IQuery.cs`
```csharp
namespace CampusHub.Common.CQRS;

public interface IQuery<TResult>
{
}
```
Both are empty markers carrying only the result type.

## `ICommandHandler.cs`
```csharp
using CampusHub.Domain.Common;

namespace CampusHub.Common.CQRS;

public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
```

## `IQueryHandler.cs`
```csharp
namespace CampusHub.Common.CQRS;

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
```

## `HandlerRegistrationExtensions.cs`
```csharp
using System.Reflection;

namespace CampusHub.Common.CQRS;

public static class HandlerRegistrationExtensions
{
    public static IServiceCollection AddHandlers(this IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                     i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)))
                .Select(i => new { HandlerType = t, InterfaceType = i }));

        foreach (var handler in handlerTypes)
        {
            services.AddTransient(handler.InterfaceType, handler.HandlerType);
        }

        return services;
    }
}
```
Call once in `Program.cs`: `builder.Services.AddHandlers(typeof(Program).Assembly);`. Every handler you write afterward is picked up automatically — no per-handler DI line. Handlers are registered **Transient**.
