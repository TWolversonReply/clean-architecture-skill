# Architecture Tests

Architecture tests enforce the dependency rule and naming conventions without
relying on project-level separation. They run as normal unit tests and fail
the build when a violation is detected.

## Preferred tool: ArchUnitNET

[ArchUnitNET](https://github.com/TNG/ArchUnitNET) (NuGet: `TngTech.ArchUnitNET`)
is the recommended library. It analyses compiled assemblies using a fluent API
and integrates with xUnit, NUnit, or MSTest.

Other options (e.g. NetArchTest) are acceptable if the team already uses them.

## Setup

Install the package that matches your test framework:

```shell
# xUnit (recommended)
dotnet add package TngTech.ArchUnitNET.xUnit

# NUnit
dotnet add package TngTech.ArchUnitNET.NUnit

# MSTest
dotnet add package TngTech.ArchUnitNET.MSTestV2
```

## Test project location

Architecture tests live in a dedicated test project or folder:

```
tests/
└── ArchitectureTests/
    └── LayerDependencyTests.cs
```

## Key concepts

### Load the architecture once

ArchUnitNET reads architecture from compiled binaries. Load assemblies once
in a static field to maximise test performance:

```csharp
private static readonly Architecture Architecture = new ArchLoader()
    .LoadAssemblies(typeof(SomeClassInYourAssembly).Assembly)
    .Build();
```

If layers live in separate assemblies, pass all of them to `LoadAssemblies`.
If layers are folders within a single assembly, load that one assembly — the
tests will select types by namespace.

### Define layers as named providers

Declare each layer as a reusable `IObjectProvider` with a descriptive `.As()`
label. This keeps individual test methods short and gives clear failure
messages:

```csharp
using static ArchUnitNET.Fluent.ArchRuleDefinition;

private readonly IObjectProvider<IType> DomainLayer =
    Types().That().ResideInNamespace("MyApp.Domain", useRegularExpressions: true)
    .As("Domain Layer");

private readonly IObjectProvider<IType> ApplicationLayer =
    Types().That().ResideInNamespace("MyApp.Application", useRegularExpressions: true)
    .As("Application Layer");

private readonly IObjectProvider<IType> InfrastructureLayer =
    Types().That().ResideInNamespace("MyApp.Infrastructure", useRegularExpressions: true)
    .As("Infrastructure Layer");

private readonly IObjectProvider<IType> PresentationLayer =
    Types().That().ResideInNamespace("MyApp.Presentation", useRegularExpressions: true)
    .As("Presentation Layer");
```

### Write rules, then check

Rules are built with the fluent API and executed with `.Check(Architecture)`:

```csharp
IArchRule rule = Types().That().Are(DomainLayer)
    .Should().NotDependOnAny(ApplicationLayer)
    .Because("Domain must not know about Application");

rule.Check(Architecture);
```

Rules can be combined with `.And()` for a single assertion covering multiple
constraints.

## Rules to enforce

### 1. Dependency direction (the core rule)

```
Domain          → depends on nothing
Application     → depends only on Domain
Infrastructure  → depends on Application (and transitively Domain)
Presentation    → depends on Application (and transitively Domain)
```

Neither Infrastructure nor Presentation should depend on each other.

### 2. Naming conventions (optional but recommended)

- Command handlers end with `CommandHandler`
- Query handlers end with `QueryHandler`
- Domain events end with `Event`
- Value objects live in the `ValueObjects` namespace

### 3. Sealed-class preference

Types that are not designed for inheritance should be sealed.

## Running

ArchUnitNET reads from debug symbols, so always run in Debug configuration:

```shell
dotnet test -c Debug --filter "FullyQualifiedName~ArchitectureTests"
```

Integrate into CI so violations are caught before merge.
