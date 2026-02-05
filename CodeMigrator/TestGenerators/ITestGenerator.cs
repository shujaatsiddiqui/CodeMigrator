using CodeMigrator.Models;

namespace CodeMigrator.TestGenerators;

/// <summary>
/// Interface for test generators that create unit tests from method metadata.
/// </summary>
public interface ITestGenerator
{
    /// <summary>
    /// Gets the name of this test generator.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the test framework name (e.g., "xUnit", "NUnit", "MSTest").
    /// </summary>
    string Framework { get; }

    /// <summary>
    /// Generates test cases for the given method metadata.
    /// </summary>
    IEnumerable<TestCase> GenerateTestCases(MethodMetadata method);

    /// <summary>
    /// Generates the complete test class code for a collection of methods.
    /// </summary>
    string GenerateTestClass(string className, IEnumerable<MethodMetadata> methods);

    /// <summary>
    /// Generates a single test method code from a test case.
    /// </summary>
    string GenerateTestMethod(TestCase testCase);

    /// <summary>
    /// Generates mock setup code for the given dependencies.
    /// </summary>
    string GenerateMockSetups(IEnumerable<DependencyInfo> dependencies);

    /// <summary>
    /// Gets the required using statements for the generated tests.
    /// </summary>
    IEnumerable<string> GetRequiredUsings();
}
