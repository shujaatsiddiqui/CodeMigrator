using CodeMigrator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeMigrator.Analyzers;

/// <summary>
/// Analyzer for ASP.NET Web Forms code-behind files.
/// </summary>
public class WebFormsAnalyzer : ICodeAnalyzer
{
    public string Name => "Web Forms Analyzer";

    public IEnumerable<string> SupportedFilePatterns => ["*.aspx.cs", "*.ascx.cs", "*.master.cs"];

    public bool CanAnalyze(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return fileName.EndsWith(".aspx.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".ascx.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".master.cs", StringComparison.OrdinalIgnoreCase);
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

        foreach (var pattern in SupportedFilePatterns)
        {
            var files = Directory.GetFiles(directoryPath, pattern, searchOption);
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fileMethods = await AnalyzeFileAsync(file, cancellationToken);
                methods.AddRange(fileMethods);
            }
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

            var methodDeclarations = classDecl.Members.OfType<MethodDeclarationSyntax>();

            foreach (var method in methodDeclarations)
            {
                var methodMetadata = ExtractMethodMetadata(method, className, namespaceName, fileName);

                // Mark Web Forms lifecycle methods
                if (IsWebFormsLifecycleMethod(method.Identifier.Text))
                {
                    methodMetadata.Documentation = $"[Web Forms Lifecycle] {methodMetadata.Documentation}";
                }

                methods.Add(methodMetadata);
            }
        }

        return Task.FromResult<IEnumerable<MethodMetadata>>(methods);
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

        // Extract parameters
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

        // Extract XML documentation if present
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

    private static bool IsWebFormsLifecycleMethod(string methodName)
    {
        var lifecycleMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Page_PreInit", "Page_Init", "Page_InitComplete",
            "Page_PreLoad", "Page_Load", "Page_LoadComplete",
            "Page_PreRender", "Page_PreRenderComplete",
            "Page_SaveStateComplete", "Page_Render", "Page_Unload",
            "OnInit", "OnLoad", "OnPreRender", "OnUnload"
        };

        return lifecycleMethods.Contains(methodName);
    }
}
