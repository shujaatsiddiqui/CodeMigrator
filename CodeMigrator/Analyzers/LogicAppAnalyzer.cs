using CodeMigrator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeMigrator.Analyzers;

/// <summary>
/// Analyzer for Azure Logic Apps Standard and Azure Functions used as Logic App actions.
/// Targets C# code with Azure Functions attributes, triggers, and Durable Functions patterns.
/// </summary>
public class LogicAppAnalyzer : ICodeAnalyzer
{
    public string Name => "Logic App Analyzer";

    public IEnumerable<string> SupportedFilePatterns => ["*.cs"];

    public bool CanAnalyze(string filePath)
    {
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
                        !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                        !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

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

            var methodDeclarations = classDecl.Members.OfType<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)));

            foreach (var method in methodDeclarations)
            {
                var functionName = GetFunctionName(method);
                if (functionName == null) continue;

                var methodMetadata = ExtractMethodMetadata(method, className, namespaceName, fileName);

                // Add function name and trigger info
                var triggerType = GetTriggerType(method);
                var functionType = GetFunctionType(method);

                var docParts = new List<string>();
                if (functionType != null) docParts.Add($"[{functionType}]");
                if (triggerType != null) docParts.Add($"[{triggerType}]");
                docParts.Add($"FunctionName: {functionName}");
                if (!string.IsNullOrEmpty(methodMetadata.Documentation))
                    docParts.Add(methodMetadata.Documentation);

                methodMetadata.Documentation = string.Join(" ", docParts);

                // Extract dependencies from constructor
                ExtractDependencies(classDecl, methodMetadata);

                methods.Add(methodMetadata);
            }
        }

        return Task.FromResult<IEnumerable<MethodMetadata>>(methods);
    }

    private static string? GetFunctionName(MethodDeclarationSyntax method)
    {
        var attributes = method.AttributeLists
            .SelectMany(al => al.Attributes);

        foreach (var attr in attributes)
        {
            var attrName = attr.Name.ToString();

            // [FunctionName("MyFunction")] or [Function("MyFunction")]
            if (attrName is "FunctionName" or "Function")
            {
                var arg = attr.ArgumentList?.Arguments.FirstOrDefault();
                if (arg != null)
                {
                    return arg.Expression.ToString().Trim('"');
                }
                return method.Identifier.Text;
            }
        }

        return null;
    }

    private static string? GetTriggerType(MethodDeclarationSyntax method)
    {
        var parameterAttributes = method.ParameterList.Parameters
            .SelectMany(p => p.AttributeLists.SelectMany(al => al.Attributes))
            .Select(a => a.Name.ToString());

        if (parameterAttributes.Any(a => a.Contains("HttpTrigger"))) return "HttpTrigger";
        if (parameterAttributes.Any(a => a.Contains("ServiceBusTrigger"))) return "ServiceBusTrigger";
        if (parameterAttributes.Any(a => a.Contains("TimerTrigger"))) return "TimerTrigger";
        if (parameterAttributes.Any(a => a.Contains("BlobTrigger"))) return "BlobTrigger";
        if (parameterAttributes.Any(a => a.Contains("QueueTrigger"))) return "QueueTrigger";
        if (parameterAttributes.Any(a => a.Contains("EventGridTrigger"))) return "EventGridTrigger";
        if (parameterAttributes.Any(a => a.Contains("EventHubTrigger"))) return "EventHubTrigger";
        if (parameterAttributes.Any(a => a.Contains("CosmosDBTrigger"))) return "CosmosDBTrigger";
        if (parameterAttributes.Any(a => a.Contains("OrchestrationTrigger"))) return "OrchestrationTrigger";
        if (parameterAttributes.Any(a => a.Contains("ActivityTrigger"))) return "ActivityTrigger";
        if (parameterAttributes.Any(a => a.Contains("EntityTrigger"))) return "EntityTrigger";

        return null;
    }

    private static string? GetFunctionType(MethodDeclarationSyntax method)
    {
        var parameterAttributes = method.ParameterList.Parameters
            .SelectMany(p => p.AttributeLists.SelectMany(al => al.Attributes))
            .Select(a => a.Name.ToString())
            .ToList();

        if (parameterAttributes.Any(a => a.Contains("OrchestrationTrigger"))) return "Orchestrator";
        if (parameterAttributes.Any(a => a.Contains("ActivityTrigger"))) return "Activity";
        if (parameterAttributes.Any(a => a.Contains("EntityTrigger"))) return "Entity";

        return null;
    }

    private static void ExtractDependencies(ClassDeclarationSyntax classDecl, MethodMetadata methodMetadata)
    {
        // Extract constructor-injected dependencies
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
