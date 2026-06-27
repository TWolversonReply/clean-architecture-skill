using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace MyApp.ArchitectureTests;

public class LayerDependencyTests
{
    // Load the architecture once — adjust the assembly reference to match your project.
    // If layers are in separate assemblies, pass all of them to LoadAssemblies.
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(typeof(Domain.Abstractions.Entity<>).Assembly)
        .Build();

    // Define layers by namespace. Adjust the root namespace to match your project.
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

    [Fact]
    public void Domain_Should_Not_Depend_On_Any_Other_Layer()
    {
        Types().That().Are(DomainLayer)
            .Should().NotDependOnAny(ApplicationLayer)
            .AndShould().NotDependOnAny(InfrastructureLayer)
            .AndShould().NotDependOnAny(PresentationLayer)
            .Because("Domain is the innermost layer and must have no outward dependencies")
            .Check(Architecture);
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Presentation()
    {
        Types().That().Are(ApplicationLayer)
            .Should().NotDependOnAny(InfrastructureLayer)
            .AndShould().NotDependOnAny(PresentationLayer)
            .Because("Application may only depend on Domain")
            .Check(Architecture);
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Presentation()
    {
        Types().That().Are(InfrastructureLayer)
            .Should().NotDependOnAny(PresentationLayer)
            .Because("Infrastructure and Presentation are sibling layers")
            .Check(Architecture);
    }

    [Fact]
    public void Presentation_Should_Not_Depend_On_Infrastructure()
    {
        Types().That().Are(PresentationLayer)
            .Should().NotDependOnAny(InfrastructureLayer)
            .Because("Infrastructure and Presentation are sibling layers")
            .Check(Architecture);
    }
}
