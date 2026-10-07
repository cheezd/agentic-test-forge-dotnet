namespace AgenticTestForge.Dry;

internal sealed record SonarSettings(string Host, string Token, string ProjectKey)
{
    public static SonarSettings? Read()
    {
        var host = Environment.GetEnvironmentVariable("SONAR_HOST_URL");
        var token = Environment.GetEnvironmentVariable("SONAR_TOKEN");
        var project = Environment.GetEnvironmentVariable("SONAR_PROJECT_KEY");
        if (
            string.IsNullOrWhiteSpace(host)
            || string.IsNullOrWhiteSpace(token)
            || string.IsNullOrWhiteSpace(project)
        )
        {
            return null;
        }

        return new SonarSettings(host.TrimEnd('/'), token, project);
    }
}

internal sealed record SonarPayload(string QualityGate, string Issues);
