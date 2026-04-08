namespace Duct.Core;

/// <summary>
/// In-memory cache for state that survives component unmount/remount.
/// Keyed by developer-provided string keys. Process-lifetime.
/// </summary>
internal static class PersistedStateCache
{
    private static readonly Dictionary<string, object?> _cache = new();

    internal static bool TryGet<T>(string key, out T value)
    {
        if (_cache.TryGetValue(key, out var boxed))
        {
            value = (T)boxed!;
            return true;
        }
        value = default!;
        return false;
    }

    internal static void Set<T>(string key, T value)
    {
        _cache[key] = value;
    }

    internal static void Remove(string key)
    {
        _cache.Remove(key);
    }

    internal static void Clear()
    {
        _cache.Clear();
    }
}
