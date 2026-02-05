namespace CodeMigrator.Models;

/// <summary>
/// Represents captured metadata about a method in the source code.
/// </summary>
public class MethodMetadata
{
    public string Name { get; set; } = string.Empty;
    public string ContainingType { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public List<ParameterInfo> Parameters { get; set; } = [];
    public List<string> Modifiers { get; set; } = [];
    public bool IsAsync { get; set; }
    public bool IsStatic { get; set; }
    public string? Documentation { get; set; }
    public List<DependencyInfo> Dependencies { get; set; } = [];
    public string SourceFilePath { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }
}

/// <summary>
/// Represents a method parameter.
/// </summary>
public class ParameterInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool HasDefaultValue { get; set; }
    public string? DefaultValue { get; set; }
}
