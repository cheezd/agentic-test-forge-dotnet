using AgenticTestForge.Cli;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Tests;

internal sealed class FakeGateRunner : IGateRunner
{
    private readonly RunResult _result;

    public FakeGateRunner(RunResult result) => _result = result;

    public bool Called { get; private set; }

    public ParsedArgs? Invocation { get; private set; }

    public RunResult Run(ParsedArgs invocation)
    {
        Called = true;
        Invocation = invocation;
        return _result;
    }
}
