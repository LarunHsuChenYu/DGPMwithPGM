using System.Reflection;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Architecture.Tests;

/// <summary>
/// 衍生守護：Domain Entity 必須 PascalCase 命名且位於 Core.Domain.Entities。
/// 不取代 LayerDependencyTests 原六條規則。
/// </summary>
public class EntityNamingTests
{
    private static readonly Assembly CoreAssembly = typeof(User).Assembly;

    [Fact]
    public void DomainEntities_ShouldBePascalCase_AndInCorrectNamespace()
    {
        var entityTypes = CoreAssembly.GetTypes()
            .Where(t => t.Namespace == "DGPM_SPM.Core.Domain.Entities")
            .Where(t => t.IsClass && !t.IsAbstract && t != typeof(BaseEntity))
            .ToList();

        entityTypes.ShouldNotBeEmpty();

        foreach (var type in entityTypes)
        {
            type.Name.ShouldNotContain("_");
            char.IsUpper(type.Name[0]).ShouldBeTrue($"{type.Name} should start with uppercase");
        }
    }
}
