using AgenticTestForge.Reporting;

namespace AgenticTestForge.Cli;

internal interface IGateRunner
{
    RunResult Run(ParsedArgs invocation);
}
