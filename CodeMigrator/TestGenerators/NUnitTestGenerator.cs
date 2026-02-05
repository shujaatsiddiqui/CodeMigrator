using System.Text;
using CodeMigrator.Models;

namespace CodeMigrator.TestGenerators;

/// <summary>
/// Generates NUnit test classes and methods.
/// </summary>
public class NUnitTestGenerator : ITestGenerator
{
    public string Name => "NUnit Test Generator";
    public string Framework => "NUnit";

    public IEnumerable<string> GetRequiredUsings() =>
    [
        "NUnit.Framework",
        "Moq",
        "FluentAssertions"
    ];

    public IEnumerable<TestCase> GenerateTestCases(MethodMetadata method)
    {
        var testCases = new List<TestCase>();

        // Happy path test
        testCases.Add(CreateTestCase(method, TestScenario.HappyPath,
            $"{method.Name}_WithValidInput_ReturnsExpectedResult"));

        // Null input tests for reference type parameters
        foreach (var param in method.Parameters.Where(p => IsNullableType(p.Type)))
        {
            testCases.Add(CreateTestCase(method, TestScenario.NullInput,
                $"{method.Name}_When{param.Name}IsNull_ThrowsArgumentNullException"));
        }

        // Empty string tests
        foreach (var param in method.Parameters.Where(p => p.Type == "string"))
        {
            testCases.Add(CreateTestCase(method, TestScenario.EmptyInput,
                $"{method.Name}_When{param.Name}IsEmpty_HandlesGracefully"));
        }

        return testCases;
    }

    private static TestCase CreateTestCase(MethodMetadata method, TestScenario scenario, string name)
    {
        return new TestCase
        {
            Name = name,
            TargetMethod = method,
            Scenario = scenario,
            MockSetups = method.Dependencies
                .Where(d => d.CanBeMocked)
                .Select(d => new MockSetup
                {
                    Dependency = d,
                    MethodName = "Setup",
                    ReturnValue = "default"
                }).ToList(),
            Act = new ActStep
            {
                MethodCall = $"_sut.{method.Name}({string.Join(", ", method.Parameters.Select(p => p.Name))})",
                IsAsync = method.IsAsync,
                ResultVariable = method.ReturnType != "void" ? "result" : null
            }
        };
    }

    private static bool IsNullableType(string type)
    {
        return type.EndsWith("?") ||
               (!type.StartsWith("int") && !type.StartsWith("bool") &&
                !type.StartsWith("double") && !type.StartsWith("float") &&
                !type.StartsWith("decimal") && !type.StartsWith("long") &&
                !type.StartsWith("short") && !type.StartsWith("byte") &&
                !type.StartsWith("char") && type != "void");
    }

    public string GenerateTestClass(string className, IEnumerable<MethodMetadata> methods)
    {
        var sb = new StringBuilder();
        var methodsList = methods.ToList();

        // Usings
        foreach (var ns in GetRequiredUsings())
        {
            sb.AppendLine($"using {ns};");
        }

        if (methodsList.Count > 0 && !string.IsNullOrEmpty(methodsList[0].Namespace))
        {
            sb.AppendLine($"using {methodsList[0].Namespace};");
        }

        sb.AppendLine();
        sb.AppendLine($"namespace {methodsList.FirstOrDefault()?.Namespace ?? "Tests"}.Tests;");
        sb.AppendLine();
        sb.AppendLine("[TestFixture]");
        sb.AppendLine($"public class {className}Tests");
        sb.AppendLine("{");

        // Fields for mocks and SUT
        var allDependencies = methodsList
            .SelectMany(m => m.Dependencies)
            .Where(d => d.CanBeMocked)
            .DistinctBy(d => d.TypeName)
            .ToList();

        foreach (var dep in allDependencies)
        {
            sb.AppendLine($"    private Mock<{dep.TypeName}> _{ToCamelCase(dep.TypeName)}Mock = null!;");
        }

        sb.AppendLine($"    private {className} _sut = null!;");
        sb.AppendLine();

        // Setup method
        sb.AppendLine("    [SetUp]");
        sb.AppendLine("    public void Setup()");
        sb.AppendLine("    {");
        foreach (var dep in allDependencies)
        {
            sb.AppendLine($"        _{ToCamelCase(dep.TypeName)}Mock = new Mock<{dep.TypeName}>();");
        }

        var constructorParams = string.Join(", ", allDependencies.Select(d => $"_{ToCamelCase(d.TypeName)}Mock.Object"));
        sb.AppendLine($"        _sut = new {className}({constructorParams});");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate test methods
        foreach (var method in methodsList)
        {
            var testCases = GenerateTestCases(method);
            foreach (var testCase in testCases)
            {
                sb.AppendLine(GenerateTestMethod(testCase));
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    public string GenerateTestMethod(TestCase testCase)
    {
        var sb = new StringBuilder();
        var method = testCase.TargetMethod;

        sb.AppendLine("    [Test]");
        sb.AppendLine($"    public {(method.IsAsync ? "async Task" : "void")} {testCase.Name}()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Arrange");

        // Generate arrange steps
        foreach (var param in method.Parameters)
        {
            var defaultValue = GetDefaultValue(param.Type);
            sb.AppendLine($"        var {param.Name} = {defaultValue};");
        }

        // Mock setups
        foreach (var mockSetup in testCase.MockSetups)
        {
            sb.AppendLine($"        // Setup for {mockSetup.Dependency.TypeName}");
        }

        sb.AppendLine();
        sb.AppendLine("        // Act");

        if (method.IsAsync)
        {
            if (testCase.Act.ResultVariable != null)
            {
                sb.AppendLine($"        var {testCase.Act.ResultVariable} = await {testCase.Act.MethodCall};");
            }
            else
            {
                sb.AppendLine($"        await {testCase.Act.MethodCall};");
            }
        }
        else
        {
            if (testCase.Act.ResultVariable != null)
            {
                sb.AppendLine($"        var {testCase.Act.ResultVariable} = {testCase.Act.MethodCall};");
            }
            else
            {
                sb.AppendLine($"        {testCase.Act.MethodCall};");
            }
        }

        sb.AppendLine();
        sb.AppendLine("        // Assert");

        if (testCase.Act.ResultVariable != null)
        {
            sb.AppendLine($"        {testCase.Act.ResultVariable}.Should().NotBeNull();");
        }
        else
        {
            sb.AppendLine("        // Add assertions here");
        }

        sb.AppendLine("    }");

        return sb.ToString();
    }

    public string GenerateMockSetups(IEnumerable<DependencyInfo> dependencies)
    {
        var sb = new StringBuilder();

        foreach (var dep in dependencies.Where(d => d.CanBeMocked))
        {
            sb.AppendLine($"var {ToCamelCase(dep.TypeName)}Mock = new Mock<{dep.TypeName}>();");
        }

        return sb.ToString();
    }

    private static string GetDefaultValue(string type) => type switch
    {
        "string" => "\"test\"",
        "int" => "1",
        "long" => "1L",
        "bool" => "true",
        "double" => "1.0",
        "float" => "1.0f",
        "decimal" => "1.0m",
        "Guid" => "Guid.NewGuid()",
        "DateTime" => "DateTime.UtcNow",
        _ when type.EndsWith("?") => "null",
        _ when type.StartsWith("List<") => $"new {type}()",
        _ when type.StartsWith("IEnumerable<") => $"Array.Empty<{type[12..^1]}>()",
        _ => $"default({type})"
    };

    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        if (str.StartsWith("I") && str.Length > 1 && char.IsUpper(str[1]))
        {
            str = str[1..];
        }
        return char.ToLowerInvariant(str[0]) + str[1..];
    }
}
