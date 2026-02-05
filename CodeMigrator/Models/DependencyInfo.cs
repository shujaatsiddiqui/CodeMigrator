namespace CodeMigrator.Models;

/// <summary>
/// Represents a dependency that needs to be mocked for testing.
/// </summary>
public class DependencyInfo
{
    public string TypeName { get; set; } = string.Empty;
    public string FullTypeName { get; set; } = string.Empty;
    public string VariableName { get; set; } = string.Empty;
    public DependencyKind Kind { get; set; }
    public List<string> UsedMethods { get; set; } = [];
    public bool IsInterface { get; set; }
    public bool CanBeMocked { get; set; } = true;
}

/// <summary>
/// Categorizes the type of dependency.
/// </summary>
public enum DependencyKind
{
    ConstructorInjected,
    MethodParameter,
    PropertyInjected,
    StaticDependency,
    FieldDependency,
    ServiceLocator
}
