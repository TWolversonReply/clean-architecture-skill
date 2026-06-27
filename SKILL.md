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
│   ├── Abstractions/        # Base types (generated from templates)
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

## Generating base types from templates

When scaffolding a new solution or when these types are missing, read the
template files from [templates/](templates/) in this skill directory and
generate each file into the appropriate project layer, adjusting the namespace
to match the target project. Do NOT create a shared library or NuGet package
for these.

| Template file | Target location |
|---------------|-----------------|
| `Entity.cs` | `Domain/Abstractions/` |
| `IDomainEvent.cs` | `Domain/Abstractions/` |
| `DomainEvent.cs` | `Domain/Abstractions/` |
| `ValueObject.cs` | `Domain/Abstractions/` |
| `Result.cs` | `Domain/Abstractions/` |
| `Error.cs` | `Domain/Abstractions/` |
| `ICommand.cs` | `Application/Abstractions/` |
| `IQuery.cs` | `Application/Abstractions/` |
| `ICommandHandler.cs` | `Application/Abstractions/` |
| `IQueryHandler.cs` | `Application/Abstractions/` |
| `IUnitOfWork.cs` | `Application/Abstractions/` |

## Default workflow

1. If base types don't exist in the solution, generate them from the templates.
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
