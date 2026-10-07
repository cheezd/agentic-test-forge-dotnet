using System.Reflection;

namespace AgenticTestForge.Cli;

internal static class ToolVersion
{
    public static string Current { get; } = Read();

    private static string Read()
    {
        var informational = typeof(ToolVersion)
            .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            return informational;
        }

        var version = typeof(ToolVersion).Assembly.GetName().Version;
        return version is null
            ? "0.0.0"
            : $"{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}";
    }
}
