# Result / Error primitives (`Domain/Common/`)

The functional error-handling core. Every domain factory, behavior method, and command handler returns `Result<T>`. Endpoints switch on `Error.Type` to pick an HTTP status.

Replace `CampusHub` with the target project's root namespace throughout.

## `ErrorType.cs`
```csharp
namespace CampusHub.Domain.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict
}
```
Add more members only when an endpoint needs to map a new HTTP status (e.g. `Unauthorized`, `Forbidden`). Keep the set small.

## `Error.cs`
```csharp
namespace CampusHub.Domain.Common;

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
}
```
`Error` is a value (record). Build it through the static factories so the `ErrorType` is pinned correctly. Use a consistent code convention — dotted `"<Aggregate>.<Reason>"` reads well.

## `Result.cs`
```csharp
namespace CampusHub.Domain.Common;

public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T? value, Error? error)
    {
        _value = value;
        _error = error;
    }

    public bool IsSuccess => _error is null;
    public bool IsFailure => _error is not null;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access the error of a successful result.");

    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(Error error) => new(default, error);

    // Lets a handler/factory `return value;` directly on the success path.
    public static implicit operator Result<T>(T value) => Success(value);
}
```
Key properties:
- Success/failure is derived from whether `_error` is null.
- Accessing `Value` on a failure (or `Error` on success) throws — surfaces misuse immediately.
- The implicit conversion from `T` is a convenience; `Result.Success(x)` / `Result.Failure(e)` are the explicit forms.

## `Unit.cs`
```csharp
namespace CampusHub.Domain.Common;

public sealed class Unit
{
    public static readonly Unit Value = new();
    private Unit() { }
}
```
Use `Unit` as the `TResult` for commands that produce no value: `PublishEventCommand : ICommand<Unit>`, handler returns `Result<Unit>`, success is `Result<Unit>.Success(Unit.Value)`.

## `IUnitOfWork.cs`
```csharp
namespace CampusHub.Domain.Common;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```
Lives in the domain so handlers depend on an abstraction. The EF `DbContext` implements it (see `efcore-persistence.md`).
