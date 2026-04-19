using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.UI.Reactor.Cli.Devtools;

/// <summary>
/// Reader-side of the spec 025 lockfile contract. Kept separate from the
/// server-side <c>LockfileRegistry</c> so the CLI doesn't pull WinUI in via a
/// project reference. The two sides agree on path + schema constants; changes
/// to the on-disk format must touch both files.
/// </summary>
internal sealed class LockfileEntry
{
    [JsonPropertyName("schema")] public string Schema { get; set; } = LockfileReader.SchemaTag;
    [JsonPropertyName("endpoint")] public string Endpoint { get; set; } = "";
    [JsonPropertyName("transport")] public string Transport { get; set; } = "http";
    [JsonPropertyName("port")] public int Port { get; set; }
    [JsonPropertyName("pid")] public int Pid { get; set; }
    [JsonPropertyName("buildTag")] public string BuildTag { get; set; } = "";
    [JsonPropertyName("project")] public string Project { get; set; } = "";
    [JsonPropertyName("startedAt")] public string StartedAt { get; set; } = "";
}

internal static class LockfileReader
{
    public const string SchemaTag = "reactor-devtools-lockfile/1";
    public const string McpSchemaTag = "reactor-devtools-mcp/1";

    public static string Directory => Path.Combine(Path.GetTempPath(), "reactor-devtools");

    public static IEnumerable<(string Path, LockfileEntry? Entry)> EnumerateAll()
    {
        if (!System.IO.Directory.Exists(Directory)) yield break;
        string[] files;
        try { files = System.IO.Directory.GetFiles(Directory, "*.json", SearchOption.TopDirectoryOnly); }
        catch { yield break; }

        foreach (var f in files)
        {
            TryRead(f, out var entry);
            yield return (f, entry);
        }
    }

    public static bool TryRead(string path, out LockfileEntry? entry)
    {
        entry = null;
        if (!File.Exists(path)) return false;
        try
        {
            var json = File.ReadAllText(path);
            entry = JsonSerializer.Deserialize<LockfileEntry>(json);
            return entry is not null;
        }
        catch
        {
            return false;
        }
    }

    public static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    /// <summary>Pid + HTTP probe. Stdio sessions are live on pid match alone.</summary>
    public static bool IsLive(LockfileEntry entry)
    {
        if (entry is null || entry.Pid <= 0) return false;
        if (!PidAlive(entry.Pid)) return false;
        if (string.Equals(entry.Transport, "stdio", StringComparison.OrdinalIgnoreCase))
            return true;
        return HttpProbe(entry.Endpoint);
    }

    private static bool PidAlive(int pid)
    {
        try
        {
            var p = global::System.Diagnostics.Process.GetProcessById(pid);
            return !p.HasExited;
        }
        catch { return false; }
    }

    private static bool HttpProbe(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint)) return false;
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(500) };
            var resp = http.GetAsync(endpoint).GetAwaiter().GetResult();
            if (!resp.IsSuccessStatusCode) return false;
            var body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("schema", out var s)) return false;
            return string.Equals(s.GetString(), McpSchemaTag, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
}
