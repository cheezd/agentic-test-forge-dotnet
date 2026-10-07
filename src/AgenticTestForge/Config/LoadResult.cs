namespace AgenticTestForge.Config;

/// <summary>Result of reading <c>forge.json</c>.</summary>
/// <param name="RepoRoot">Directory that contains the file, when loading succeeded.</param>
/// <param name="Config">Settings after defaults, when loading succeeded.</param>
/// <param name="Error">Tool error. The process exits 2.</param>
internal sealed record LoadResult(string? RepoRoot, ForgeConfig? Config, string? Error)
{
    public static LoadResult Ok(string repoRoot, ForgeConfig config) => new(repoRoot, config, null);

    public static LoadResult Failed(string error) => new(null, null, error);
}
