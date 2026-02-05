using CodeMigrator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeMigrator.Analyzers;

/// <summary>
/// Analyzer for ASP.NET Web API controllers.
/// </summary>
public class WebApiAnalyzer : ICodeAnalyzer
{
    public string Name => "Web API Analyzer";

    public IEnumerable<string> SupportedFilePatterns => ["*Controller.cs", "*ApiController.cs"];

    public bool CanAnalyze(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return fileName.EndsWith("Controller.cs", StringComparison.OrdinalIgnoreCase);
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

        var files = Directory.GetFiles(directoryPath, "*Controller.cs", searchOption);
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

        var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(c => IsApiController(c));

        foreach (var classDecl in classDeclarations)
        {
            var namespaceName = GetNamespace(classDecl);
            var className = classDecl.Identifier.Text;

            var methodDeclarations = classDecl.Members.OfType<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)));

            foreach (var method in methodDeclarations)
            {
                var methodMetadata = ExtractMethodMetadata(method, className, namespaceName, fileName);

                // Add HTTP method info from attributes
                var httpMethod = GetHttpMethod(method);
                if (!string.IsNullOrEmpty(httpMethod))
                {
                    methodMetadata.Documentation = $"[{httpMethod}] {methodMetadata.Documentation}";
                }

                // Extract dependencies from constructor
                ExtractDependencies(classDecl, methodMetadata);

                methods.Add(methodMetadata);
            }
        }

        return Task.FromResult<IEnumerable<MethodMetadata>>(methods);
    }

    private static bool IsApiController(ClassDeclarationSyntax classDecl)
    {
        // Check if class inherits from ApiController or Controller
        if (classDecl.BaseList == null) return false;

        var baseTypes = classDecl.BaseList.Types.Select(t => t.ToString());
        return baseTypes.Any(t => t.Contains("ApiController") ||
                                   t.Contains("Controller") ||
                                   t.Contains("ControllerBase"));
    }

    private static string? GetHttpMethod(MethodDeclarationSyntax method)
    {
        var attributes = method.AttributeLists
            .SelectMany(al => al.Attributes)
            .Select(a => a.Name.ToString());

        if (attributes.Any(a => a.Contains("HttpGet"))) return "GET";
        if (attributes.Any(a => a.Contains("HttpPost"))) return "POST";
        if (attributes.Any(a => a.Contains("HttpPut"))) return "PUT";
        if (attributes.Any(a => a.Contains("HttpDelete"))) return "DELETE";
        if (attributes.Any(a => a.Contains("HttpPatch"))) return "PATCH";

        return null;
    }

    private static void ExtractDependencies(ClassDeclarationSyntax classDecl, MethodMetadata methodMetadata)
    {
        // Find constructor and extract injected dependencies
        var constructor = classDecl.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
        if (constructor == null) return;

        foreach (var param in constructor.ParameterList.Parameters)
        {
            var typeName = param.Type?.ToString() ?? "object";
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
