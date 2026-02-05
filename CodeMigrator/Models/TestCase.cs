namespace CodeMigrator.Models;

/// <summary>
/// Represents a generated test case.
/// </summary>
public class TestCase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MethodMetadata TargetMethod { get; set; } = null!;
    public TestScenario Scenario { get; set; }
    public List<MockSetup> MockSetups { get; set; } = [];
    public List<ArrangeStep> ArrangeSteps { get; set; } = [];
    public ActStep Act { get; set; } = null!;
    public List<AssertStep> AssertSteps { get; set; } = [];
}

/// <summary>
/// Defines the scenario being tested.
/// </summary>
public enum TestScenario
{
    HappyPath,
    NullInput,
    EmptyInput,
    InvalidInput,
    ExceptionThrown,
    EdgeCase,
    BoundaryCondition
}

/// <summary>
/// Represents a mock setup for a dependency.
/// </summary>
public class MockSetup
{
    public DependencyInfo Dependency { get; set; } = null!;
    public string MethodName { get; set; } = string.Empty;
    public string ReturnValue { get; set; } = string.Empty;
    public List<string> ParameterMatchers { get; set; } = [];
}

/// <summary>
/// Represents an arrange step in the test.
/// </summary>
public class ArrangeStep
{
    public string VariableName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Comment { get; set; }
}

/// <summary>
/// Represents the act step in the test.
/// </summary>
public class ActStep
{
    public string MethodCall { get; set; } = string.Empty;
    public string? ResultVariable { get; set; }
    public bool IsAsync { get; set; }
    public bool ExpectsException { get; set; }
    public string? ExpectedExceptionType { get; set; }
}

/// <summary>
/// Represents an assertion in the test.
/// </summary>
public class AssertStep
{
    public AssertionType Type { get; set; }
    public string Expected { get; set; } = string.Empty;
    public string Actual { get; set; } = string.Empty;
    public string? Message { get; set; }
}

/// <summary>
/// Types of assertions.
/// </summary>
public enum AssertionType
{
    Equal,
    NotEqual,
    True,
    False,
    Null,
    NotNull,
    Throws,
    Contains,
    Empty,
    NotEmpty
}
