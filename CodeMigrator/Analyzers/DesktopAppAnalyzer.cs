using CodeMigrator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeMigrator.Analyzers;

/// <summary>
/// Analyzer for Windows Forms and WPF desktop applications.
/// </summary>
public class DesktopAppAnalyzer : ICodeAnalyzer
{
    public string Name => "Desktop Application Analyzer";

    public IEnumerable<string> SupportedFilePatterns => ["*.cs"];

    public bool CanAnalyze(string filePath)
    {
        // Analyze any .cs file, but focus on forms and windows
        return filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IEnumerable<MethodMetadata>> AnalyzeFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        return await AnalyzeContentAsync(content, filePath, cancellationToken);
    }

    public async Task<IEnumerable<MethodMetadata>> AnalyzeDirectoryAsync(string directoryPath, bool recursive = true, CancellationToken cancellationToken = default)
    {
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var methods = new List<MethodMetadata>();

        var files = Directory.GetFiles(directoryPath, "*.cs", searchOption)
            .Where(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) &&
                        !f.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileMethods = await AnalyzeFileAsync(file, cancellationToken);
            methods.AddRange(fileMethods);
        }

        return methods;
    }

    public Task<IEnumerable<MethodMetadata>> AnalyzeContentAsync(string content, string fileName = "source.cs", CancellationToken cancellationToken = default)
    {
        var tree = CSharpSyntaxTree.ParseText(content, cancellationToken: cancellationToken);
        var root = tree.GetRoot(cancellationToken);
        var methods = new List<MethodMetadata>();

        var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

        foreach (var classDecl in classDeclarations)
        {
            var namespaceName = GetNamespace(classDecl);
            var className = classDecl.Identifier.Text;
            var classType = DetermineClassType(classDecl);

            var methodDeclarations = classDecl.Members.OfType<MethodDeclarationSyntax>();

            foreach (var method in methodDeclarations)
            {
                var methodMetadata = ExtractMethodMetadata(method, className, namespaceName, fileName);

                // Add class type and event handler info
                if (classType != DesktopClassType.Regular)
                {
                    methodMetadata.Documentation = $"[{classType}] {methodMetadata.Documentation}";
                }

                if (IsEventHandler(method))
                {
                    methodMetadata.Documentation = $"[EventHandler] {methodMetadata.Documentation}";
                }

                // Extract dependencies
                ExtractDependencies(classDecl, method, methodMetadata);

                methods.Add(methodMetadata);
            }
        }

        return Task.FromResult<IEnumerable<MethodMetadata>>(methods);
    }

    private enum DesktopClassType
    {
        Regular,
        WinForm,
        WpfWindow,
        WpfUserControl,
        WpfPage
    }

    private static DesktopClassType DetermineClassType(ClassDeclarationSyntax classDecl)
    {
        if (classDecl.BaseList == null) return DesktopClassType.Regular;

        var baseTypes = classDecl.BaseList.Types.Select(t => t.ToString()).ToList();

        if (baseTypes.Any(t => t.Contains("Form"))) return DesktopClassType.WinForm;
        if (baseTypes.Any(t => t.Contains("Window"))) return DesktopClassType.WpfWindow;
        if (baseTypes.Any(t => t.Contains("UserControl"))) return DesktopClassType.WpfUserControl;
        if (baseTypes.Any(t => t.Contains("Page"))) return DesktopClassType.WpfPage;

        return DesktopClassType.Regular;
    }

    private static bool IsEventHandler(MethodDeclarationSyntax method)
    {
        // Check for event handler signature: (object sender, EventArgs e)
        var parameters = method.ParameterList.Parameters;
        if (parameters.Count != 2) return false;

        var firstParam = parameters[0].Type?.ToString() ?? "";
        var secondParam = parameters[1].Type?.ToString() ?? "";

        return (firstParam == "object" || firstParam == "object?") &&
               secondParam.EndsWith("EventArgs");
    }

    private static void ExtractDependencies(ClassDeclarationSyntax classDecl, MethodDeclarationSyntax method, MethodMetadata methodMetadata)
    {
        // Extract field dependencies
        var fields = classDecl.Members.OfType<FieldDeclarationSyntax>();
        foreach (var field in fields)
        {
            var typeName = field.Declaration.Type.ToString();
            if (ShouldIncludeAsDependency(typeName))
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    methodMetadata.Dependencies.Add(new DependencyInfo
                    {
                        TypeName = typeName,
                        FullTypeName = typeName,
                        VariableName = variable.Identifier.Text,
                        Kind = DependencyKind.FieldDependency,
                        IsInterface = typeName.StartsWith("I") && char.IsUpper(typeName.ElementAtOrDefault(1)),
                        CanBeMocked = typeName.StartsWith("I") && char.IsUpper(typeName.ElementAtOrDefault(1))
                    });
                }
            }
        }

        // Extract constructor-injected dependencies
        var constructor = classDecl.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
        if (constructor != null)
        {
            foreach (var param in constructor.ParameterList.Parameters)
            {
                var typeName = param.Type?.ToString() ?? "object";
                if (ShouldIncludeAsDependency(typeName))
                {
                    var isInterface = typeName.StartsWith("I") && char.IsUpper(typeName.ElementAtOrDefault(1));
                    methodMetadata.Dependencies.Add(new DependencyInfo
                    {
                        TypeName = typeName,
                        FullTypeName = typeName,
                        VariableName = param.Identifier.Text,
                        Kind = DependencyKind.ConstructorInjected,
                        IsInterface = isInterface,
                        CanBeMocked = isInterface
                    });
                }
            }
        }
    }

    private static bool ShouldIncludeAsDependency(string typeName)
    {
        // Exclude common UI controls and primitives
        var excludedTypes = new HashSet<string>
        {
            "string", "int", "bool", "double", "float", "decimal",
            "Button", "Label", "TextBox", "ComboBox", "ListBox",
            "Panel", "GroupBox", "TabControl", "DataGridView",
            "Timer", "ToolTip", "ContextMenuStrip"
        };

        return !excludedTypes.Contains(typeName) &&
               !typeName.StartsWith("System.Windows.Forms.") &&
               !typeName.StartsWith("System.Windows.Controls.");
    }

    private static MethodMetadata ExtractMethodMetadata(MethodDeclarationSyntax method, string className, string namespaceName, string filePath)
    {
        var metadata = new MethodMetadata
        {
            Name = method.Identifier.Text,
            ContainingType = className,
            Namespace = namespaceName,
            ReturnType = method.ReturnType.ToString(),
            IsAsync = method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)),
            IsStatic = method.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)),
            Modifiers = method.Modifiers.Select(m => m.Text).ToList(),
            SourceFilePath = filePath,
            StartLine = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
            EndLine = method.GetLocation().GetLineSpan().EndLinePosition.Line + 1
        };

        foreach (var param in method.ParameterList.Parameters)
        {
            metadata.Parameters.Add(new Models.ParameterInfo
            {
                Name = param.Identifier.Text,
                Type = param.Type?.ToString() ?? "object",
                HasDefaultValue = param.Default != null,
                DefaultValue = param.Default?.Value.ToString()
            });
        }

        var trivia = method.GetLeadingTrivia()
            .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                                  t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
        if (trivia != default)
        {
            metadata.Documentation = trivia.ToString().Trim();
        }

        return metadata;
    }

    private static string GetNamespace(SyntaxNode node)
    {
        var namespaceDecl = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        return namespaceDecl?.Name.ToString() ?? string.Empty;
    }
}
