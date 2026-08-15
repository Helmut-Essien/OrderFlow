namespace OrderFlow.Api.Hosting;

/// <summary>
/// Resolves CORS origins from <c>Cors:Origins</c> as a JSON array or a comma-separated <c>CORS__ORIGINS</c> env var.
/// </summary>
public static class CorsOrigins
{
    /// <summary>
    /// Returns trimmed origin URLs. Empty means same-origin only (no browser cross-origin calls).
    /// </summary>
    public static string[] Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection("Cors:Origins");
        var fromArray = section.Get<string[]>();
        if (fromArray is { Length: > 0 })
            return Normalize(fromArray);

        // Env vars are a single string (`CORS__ORIGINS=https://a,https://b`); JSON config is a string array.
        var raw = configuration["Cors:Origins"];
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        return Normalize(raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string[] Normalize(IEnumerable<string> origins) =>
        origins
            .Select(origin => origin.Trim())
            .Where(origin => origin.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
