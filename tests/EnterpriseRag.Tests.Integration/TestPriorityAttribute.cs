namespace EnterpriseRag.Tests.Integration;

/// <summary>
/// Attribute to control test execution order within a class.
/// Lower values run first. Used to minimize Ollama model swaps
/// by grouping embedding tests before generation tests.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TestPriorityAttribute : Attribute
{
    public int Priority { get; }
    public TestPriorityAttribute(int priority) => Priority = priority;
}
