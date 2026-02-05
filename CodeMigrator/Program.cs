using CodeMigrator.Analyzers;
using CodeMigrator.TestGenerators;

if (args.Length == 0)
{
    ShowHelp();
    return 0;
}

var command = args[0].ToLower();

return command switch
{
    "analyze" => await HandleAnalyze(args.Skip(1).ToArray()),
    "generate" => await HandleGenerate(args.Skip(1).ToArray()),
    "help" or "--help" or "-h" => ShowHelp(),
    _ => ShowHelp()
};

static int ShowHelp()
{
    Console.WriteLine("Test-Driven Migrator - Analyze legacy .NET code and generate unit tests");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  codemigrator analyze <path> [options]");
    Console.WriteLine("  codemigrator generate <source> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  analyze    Analyze source code and extract method metadata");
    Console.WriteLine("  generate   Generate unit tests from analyzed code");
    Console.WriteLine();
    Console.WriteLine("Analyze Options:");
    Console.WriteLine("  --type <type>       Project type: webforms, webapi, desktop, logicapp, or auto (default: auto)");
    Console.WriteLine("  --output <file>     Output file for analysis results (JSON)");
    Console.WriteLine("  --no-recursive      Do not analyze directories recursively");
    Console.WriteLine();
    Console.WriteLine("Generate Options:");
    Console.WriteLine("  --framework <fw>    Test framework: xunit or nunit (default: xunit)");
    Console.WriteLine("  --output <dir>      Output directory for generated tests");
    Console.WriteLine("  --type <type>       Project type: webforms, webapi, desktop, logicapp, or auto");
    return 0;
}

static async Task<int> HandleAnalyze(string[] args)
{
    if (args.Length == 0)
    {
        Console.WriteLine("Error: Path is required");
        return 1;
    }

    var path = args[0];
    var type = GetArgValue(args, "--type") ?? "auto";
    var output = GetArgValue(args, "--output");
    var recursive = !args.Contains("--no-recursive");

    Console.WriteLine($"Analyzing: {path}");
    Console.WriteLine($"Type: {type}");

    ICodeAnalyzer analyzer = type.ToLower() switch
    {
        "webforms" => new WebFormsAnalyzer(),
        "webapi" => new WebApiAnalyzer(),
        "desktop" => new DesktopAppAnalyzer(),
        "logicapp" => new LogicAppAnalyzer(),
        _ => DetectAnalyzer(path)
    };

    Console.WriteLine($"Using analyzer: {analyzer.Name}");

    try
    {
        var methods = Directory.Exists(path)
            ? await analyzer.AnalyzeDirectoryAsync(path, recursive)
            : await analyzer.AnalyzeFileAsync(path);

        var methodList = methods.ToList();
        Console.WriteLine($"Found {methodList.Count} methods");

        foreach (var method in methodList)
        {
            Console.WriteLine($"  - {method.ContainingType}.{method.Name}({string.Join(", ", method.Parameters.Select(p => p.Type))})");
            if (method.Dependencies.Count > 0)
            {
                Console.WriteLine($"    Dependencies: {string.Join(", ", method.Dependencies.Select(d => d.TypeName))}");
            }
        }

        if (!string.IsNullOrEmpty(output))
        {
            var json = System.Text.Json.JsonSerializer.Serialize(methodList, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(output, json);
            Console.WriteLine($"Results saved to: {output}");
        }

        return 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        return 1;
    }
}

static async Task<int> HandleGenerate(string[] args)
{
    if (args.Length == 0)
    {
        Console.WriteLine("Error: Source path is required");
        return 1;
    }

    var source = args[0];
    var framework = GetArgValue(args, "--framework") ?? "xunit";
    var testOutput = GetArgValue(args, "--output") ?? Path.Combine(Directory.GetCurrentDirectory(), "GeneratedTests");
    var type = GetArgValue(args, "--type") ?? "auto";

    Console.WriteLine($"Generating tests for: {source}");
    Console.WriteLine($"Framework: {framework}");
    Console.WriteLine($"Output: {testOutput}");

    ICodeAnalyzer analyzer = type.ToLower() switch
    {
        "webforms" => new WebFormsAnalyzer(),
        "webapi" => new WebApiAnalyzer(),
        "desktop" => new DesktopAppAnalyzer(),
        "logicapp" => new LogicAppAnalyzer(),
        _ => DetectAnalyzer(source)
    };

    ITestGenerator generator = framework.ToLower() switch
    {
        "nunit" => new NUnitTestGenerator(),
        _ => new XUnitTestGenerator()
    };

    Console.WriteLine($"Using analyzer: {analyzer.Name}");
    Console.WriteLine($"Using generator: {generator.Name}");

    try
    {
        var methods = Directory.Exists(source)
            ? await analyzer.AnalyzeDirectoryAsync(source)
            : await analyzer.AnalyzeFileAsync(source);

        var methodsByClass = methods.GroupBy(m => m.ContainingType);

        Directory.CreateDirectory(testOutput);

        foreach (var classGroup in methodsByClass)
        {
            var className = classGroup.Key;
            var testCode = generator.GenerateTestClass(className, classGroup);
            var testFilePath = Path.Combine(testOutput, $"{className}Tests.cs");

            await File.WriteAllTextAsync(testFilePath, testCode);
            Console.WriteLine($"Generated: {testFilePath}");
        }

        Console.WriteLine("Test generation complete!");
        return 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        return 1;
    }
}

static string? GetArgValue(string[] args, string name)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == name)
        {
            return args[i + 1];
        }
    }
    return null;
}

static ICodeAnalyzer DetectAnalyzer(string path)
{
    if (Directory.Exists(path))
    {
        if (Directory.GetFiles(path, "*.aspx.cs", SearchOption.AllDirectories).Length > 0)
            return new WebFormsAnalyzer();

        if (Directory.GetFiles(path, "*Controller.cs", SearchOption.AllDirectories).Length > 0)
            return new WebApiAnalyzer();

        // Detect Logic App / Azure Functions projects by host.json or FunctionName attributes
        if (File.Exists(Path.Combine(path, "host.json")) ||
            Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
                .Any(f => File.ReadAllText(f).Contains("[FunctionName") || File.ReadAllText(f).Contains("[Function(")))
            return new LogicAppAnalyzer();
    }
    else if (File.Exists(path))
    {
        var fileName = Path.GetFileName(path);
        if (fileName.EndsWith(".aspx.cs") || fileName.EndsWith(".ascx.cs"))
            return new WebFormsAnalyzer();
        if (fileName.EndsWith("Controller.cs"))
            return new WebApiAnalyzer();

        var content = File.ReadAllText(path);
        if (content.Contains("[FunctionName") || content.Contains("[Function("))
            return new LogicAppAnalyzer();
    }

    return new DesktopAppAnalyzer();
}
