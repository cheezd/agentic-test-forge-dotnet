namespace AgenticTestForge.Dry;

/// <summary>Registered duplication sources. Add a source here to make its id selectable.</summary>
internal static class DrySourceCatalog
{
    private static readonly IDrySource[] Registered = [new SonarDrySource()];

    public static IReadOnlyList<IDrySource> Select(IReadOnlyList<string> ids)
    {
        var selected = new List<IDrySource>();
        foreach (var id in ids)
        {
            var source = Find(id);
            if (source is not null)
            {
                selected.Add(source);
            }
        }

        return selected;
    }

    private static IDrySource? Find(string id)
    {
        foreach (var source in Registered)
        {
            if (source.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            {
                return source;
            }
        }

        return null;
    }
}
