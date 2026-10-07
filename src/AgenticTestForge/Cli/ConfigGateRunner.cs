using AgenticTestForge.Config;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Cli;

/// <summary>Loads <c>forge.json</c>, applies the shared flags, and stops when the run cannot be resolved.</summary>
internal sealed class ConfigGateRunner : IGateRunner
{
    private readonly string _startDirectory;

    public ConfigGateRunner()
        : this(Directory.GetCurrentDirectory()) { }

    public ConfigGateRunner(string startDirectory) => _startDirectory = startDirectory;

    public RunResult Run(ParsedArgs invocation)
    {
        var resolved = RunPlanResolver.Resolve(invocation, _startDirectory);
        if (resolved.Error is not null || resolved.Plan is null)
        {
            return CheckReportBuilder.ForToolError(
                invocation,
                resolved.Error ?? "forge.json was not found."
            );
        }

        return QualityGatePipeline.Run(resolved.Plan);
    }
}
