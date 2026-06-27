---
name: clean-architecture
description: >-
    Use when scaffolding or developing .NET solutions following Clean Architecture
    (domain-centric, CQRS, Result pattern). Use when the user mentions Clean
    Architecture, CQRS, domain layer, application layer, commands/queries, domain
    events, or wants to add a new feature/entity following this layered architecture
    style. Do not use for simple CRUD apps, minimal APIs without layering, or
    non-.NET projects.
---

# Clean Architecture Skill

Use this skill when creating or modifying .NET solutions that follow Clean Architecture
with CQRS and domain-driven design patterns. Each project owns its own base types —
do not reference a shared abstractions library.

## Use this skill for

- Scaffolding new Clean Architecture solutions
- Generating base types into the Domain layer from templates
- Adding new features (commands, queries, handlers)
- Creating domain entities, value objects, and domain events
- Configuring persistence and infrastructure

## Do not use this skill for

- Simple CRUD applications without layering
- Minimal API projects that don't need domain separation
- Non-.NET projects

## Solution structure

```
src/
├── Domain/
│   ├── Abstractions/        # Base types (generated from templates below)
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Events/
│   └── Errors/
├── Application/
│   ├── Abstractions/        # IUnitOfWork, IClock, etc.
│   ├── [Feature]/
│   │   ├── Commands/
│   │   └── Queries/
│   └── Behaviors/
├── Infrastructure/
│   ├── Persistence/
│   ├── Services/
│   └── DependencyInjection.cs
└── Presentation/
    └── Endpoints/
```

## Base types — generate from templates

When scaffolding a new solution or when these types are missing, generate them
directly into `Domain/Abstractions/` and `Application/Abstractions/`. Do NOT
create a shared library or NuGet package for these.

### Domain/Abstractions/Entity.cs

```csharp
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity(TId id) => Id = id;
    protected Entity() { } // EF Core

    public TId Id { get; init; }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    public bool Equals(Entity<TId>? other) =>
        other is not null && Id.Equals(other.Id);

    public override bool Equals(object? obj) =>
        obj is Entity<TId> entity && Equals(entity);

    public override int GetHashCode() => Id.GetHashCode();
}
```

### Domain/Abstractions/IDomainEvent.cs

```csharp
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOnUtc { get; }
}
```

### Domain/Abstractions/DomainEvent.cs

```csharp
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
```

### Domain/Abstractions/ValueObject.cs

```csharp
public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object> GetAtomicValues();

    public bool Equals(ValueObject? other) =>
        other is not null &&
        GetAtomicValues().SequenceEqual(other.GetAtomicValues());

    public override bool Equals(object? obj) =>
        obj is ValueObject vo && Equals(vo);

    public override int GetHashCode() =>
        GetAtomicValues().Aggregate(0, HashCode.Combine);
}
```

### Domain/Abstractions/Result.cs

```csharp
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException();
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException();

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) => _value = value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("No value on failure result.");
}
```

### Domain/Abstractions/Error.cs

```csharp
public sealed record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.");
}
```

### Application/Abstractions/ICommand.cs

```csharp
public interface ICommand<TResponse>
{
    // Marker interface — represents a state-changing use case
}
```

### Application/Abstractions/IQuery.cs

```csharp
public interface IQuery<TResponse>
{
    // Marker interface — represents a read-only use case
}
```

### Application/Abstractions/ICommandHandler.cs

```csharp
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken ct = default);
}
```

### Application/Abstractions/IQueryHandler.cs

```csharp
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken ct = default);
}
```

### Application/Abstractions/IUnitOfWork.cs

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

## Default workflow

1. If base types don't exist in the solution, generate them from the templates above.
2. Identify which layer(s) the change touches.
3. Work outward: Domain → Application → Infrastructure → Presentation.
4. For a new feature, create in order:
   - Domain entity/value object (if new)
   - Command or Query record implementing `ICommand<T>` or `IQuery<T>`
   - Handler class implementing `ICommandHandler` or `IQueryHandler`
   - Infrastructure persistence (EF config, repository if needed)
   - API endpoint that instantiates and calls the handler directly
5. Use the Result pattern — never throw exceptions for business rule violations.
6. Register handlers in DI as scoped services.

## Key rules

- Never reference Infrastructure or Presentation from Domain or Application.
- Application depends only on Domain.
- All external dependencies are behind interfaces defined in Application.
- Base types are owned by each project — never import them from a shared package.
- Use sealed classes where inheritance isn't needed.
- One file per class; filename matches class name.
- Handlers are invoked directly — no mediator library unless explicitly requested.
