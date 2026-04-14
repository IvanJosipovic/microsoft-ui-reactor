namespace HeadTrax;

/// <summary>
/// Runtime configuration for the HeadTrax sample app.
/// Set via command-line args or defaults.
/// </summary>
internal static class AppConfig
{
    private static string? _sqliteDbPath;

    /// <summary>Path to the SQLite database file. Lazily resolved on first access.</summary>
    public static string SqliteDbPath
    {
        get => _sqliteDbPath ??= FindDbPath();
        set => _sqliteDbPath = value;
    }

    private static string FindDbPath()
    {
        // Walk up from the exe directory until we find the service/ folder
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "service", "headtrax.db");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }

        // Fallback: relative to exe (works from dotnet run in project dir)
        return Path.Combine(AppContext.BaseDirectory, "service", "headtrax.db");
    }

    /// <summary>GraphQL endpoint URL for the Node.js service.</summary>
    public static string GraphQLUrl { get; set; } = "http://localhost:4000/graphql";
}
