using AgenticTestForge.Config;

namespace AgenticTestForge.Dry;

internal sealed class SonarDrySource : IDrySource
{
    public const string IdValue = "sonar";

    private readonly Func<string, bool> _available;
    private readonly Func<SonarSettings?> _settings;
    private readonly Func<SonarSettings, string, SonarPayload?> _fetch;

    public SonarDrySource()
        : this(
            SonarProcess.IsAvailable,
            SonarSettings.Read,
            static (settings, root) => SonarProcess.Fetch(settings, root)
        ) { }

    internal SonarDrySource(
        Func<string, bool> available,
        Func<SonarSettings?> settings,
        Func<SonarSettings, string, SonarPayload?> fetch
    )
    {
        _available = available;
        _settings = settings;
        _fetch = fetch;
    }

    public string Id => IdValue;

    public DryCollection Collect(RunPlan plan)
    {
        if (!_available(plan.RepoRoot))
        {
            return DryCollection.NotRun;
        }

        var settings = _settings();
        if (settings is null)
        {
            return DryCollection.NotRun;
        }

        var payload = _fetch(settings, plan.RepoRoot);
        return payload is null ? DryCollection.NotRun : SonarReport.Read(payload);
    }
}
