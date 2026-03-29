using Xunit.Abstractions;
using Xunit.Sdk;

namespace EnterpriseRag.Tests.Integration;

/// <summary>
/// xUnit test case orderer that sorts by [TestPriority(N)].
/// Tests without the attribute default to priority 0.
/// </summary>
public class PriorityOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
    {
        return testCases.OrderBy(tc =>
        {
            var attr = tc.TestMethod.Method
                .GetCustomAttributes(typeof(TestPriorityAttribute).AssemblyQualifiedName!)
                .FirstOrDefault();

            return attr?.GetNamedArgument<int>("Priority") ?? 0;
        });
    }
}
