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

Layers are logical boundaries — they do not have to map 1:1 to separate projects.
A single project with folder-based separation is a valid starting point; the
dependency rule (Domain ← Application ← Infrastructure/Presentation) can be
enforced with architecture tests (e.g. NetArchTest, ArchUnitNET) rather than
project references.

When discussing structure with the user, explain the trade-offs:
- **Folder-based (single project)**: Lower ceremony, faster to scaffold, suits
  smaller bounded contexts. Enforce layering via architecture tests.
- **Project-based (multi-project)**: Compiler-enforced dependency boundaries,
  suits larger teams or independently deployable modules.

Prefer folder-based unless the user explicitly requests separate projects.

### Folder layout (single project or per-layer projects)

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
| `LayerDependencyTests.cs` | `tests/ArchitectureTests/` |

## Architecture tests

The dependency rule is enforced with architecture tests, not project references.
For detailed guidance on setup, tooling, and the rules to implement, see
[references/architecture-tests.md](references/architecture-tests.md).

When scaffolding a new solution, generate `LayerDependencyTests.cs` from the
template into a test project and install `TngTech.ArchUnitNET.xUnit` (or the
equivalent for the team's test framework). Adjust namespaces in the template
to match the target project.

## Bootstrapping with Aspire

New solutions should be bootstrapped and configured using **Aspire**. Aspire
provides the AppHost that orchestrates local development — databases, caches,
messaging, and service discovery are all modeled in one place.

### How this skill interacts with the Aspire skills

This skill owns the Clean Architecture structure and patterns. For Aspire
operations, delegate to the appropriate Aspire skill:

| Task | Delegate to |
|------|-------------|
| Initialize Aspire in the solution | `aspire init` via the **aspire** skill |
| Wire up the AppHost after init | The **aspireify** skill |
| Start/stop/inspect resources | The **aspire** skill |
| Add integrations (Postgres, Redis, etc.) | `aspire add` via the **aspire** skill |

### Bootstrapping a new Clean Architecture solution

When creating a new solution from scratch:

1. Create the solution and project structure (folders or projects per the
   user's preference).
2. Generate base types from [templates/](templates/).
3. Run `aspire init` to drop the AppHost skeleton into the solution.
4. Hand off to the **aspireify** skill to complete AppHost wiring — it will
   model the API project, add database resources, configure service discovery,
   and set up dev certificates.
5. Use `aspire add` to bring in integrations the solution needs (e.g.,
   `Aspire.Hosting.PostgreSQL`, `Aspire.Hosting.Redis`).
6. Use `aspire start` to verify the full stack runs locally.

### Infrastructure layer and Aspire integrations

When Aspire manages a resource (e.g., a PostgreSQL container), the
Infrastructure layer still owns the EF Core `DbContext` and repository
implementations. The difference is that **connection strings come from Aspire**
via standard .NET configuration (`ConnectionStrings:__resourcename`) rather
than being hardcoded or manually configured.

This means:
- Infrastructure registers its `DbContext` reading from `IConfiguration`
- The AppHost wires resources with `WithReference(db)` so connection strings
  are injected automatically
- No connection strings in `appsettings.json` for local dev — Aspire handles it
- Production deployment uses the same configuration keys, provisioned differently

### When Aspire is not available

If the user's environment does not have the Aspire CLI or they explicitly opt
out, fall back to manual configuration (connection strings in appsettings,
Docker Compose, etc.). The Clean Architecture patterns remain the same — only
the bootstrapping and local orchestration differ.

## Default workflow

1. Bootstrap the solution with Aspire (if available): `aspire init`, then
   delegate to the **aspireify** skill to complete wiring.
2. If base types don't exist in the solution, generate them from the templates.
3. Identify which layer(s) the change touches.
4. Work outward: Domain → Application → Infrastructure → Presentation.
5. For a new feature, create in order:
   - Domain entity/value object (if new)
   - Command or Query record implementing `ICommand<T>` or `IQuery<T>`
   - Handler class implementing `ICommandHandler` or `IQueryHandler`
   - Infrastructure persistence (EF config, repository if needed)
   - API endpoint that instantiates and calls the handler directly
6. Use the Result pattern — never throw exceptions for business rule violations.
7. Register handlers in DI as scoped services.
8. Use `aspire start` to verify changes work end-to-end locally.

## Key rules

- The dependency rule flows inward: Domain knows nothing; Application depends
  only on Domain; Infrastructure and Presentation depend on Application.
- Enforce this via architecture tests — project separation is optional.
- All external dependencies are behind interfaces defined in Application.
- Base types are owned by each project — never import them from a shared package.
- Use sealed classes where inheritance isn't needed.
- One file per class; filename matches class name.
- Handlers are invoked directly — no mediator library unless explicitly requested.
