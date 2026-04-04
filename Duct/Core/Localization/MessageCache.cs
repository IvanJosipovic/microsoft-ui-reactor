using System.Collections.Concurrent;
using System.Globalization;
using Jeffijoe.MessageFormat;

namespace Duct.Core.Localization;

/// <summary>
/// Caches MessageFormatter instances per locale. Each formatter internally caches
/// compiled ICU patterns, so we only need one instance per locale.
/// </summary>
public sealed class MessageCache
{
    private readonly ConcurrentDictionary<string, MessageFormatter> _formatters = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or creates a MessageFormatter for the given locale.
    /// </summary>
    public MessageFormatter GetFormatter(string locale)
    {
        return _formatters.GetOrAdd(locale, loc =>
            new MessageFormatter(useCache: true, culture: new CultureInfo(loc), customValueFormatter: null));
    }

    /// <summary>
    /// Formats an ICU pattern with the given arguments for the specified locale.
    /// </summary>
    public string Format(string locale, string pattern, IDictionary<string, object>? args = null)
    {
        var formatter = GetFormatter(locale);
        var dict = (IReadOnlyDictionary<string, object?>)(args is null || args.Count == 0
            ? new Dictionary<string, object?>()
            : args.ToDictionary(kv => kv.Key, kv => (object?)kv.Value));
        return formatter.FormatMessage(pattern, dict);
    }

    /// <summary>
    /// Flushes all cached formatters (e.g., on full locale switch).
    /// </summary>
    public void Flush()
    {
        _formatters.Clear();
    }

    /// <summary>
    /// Flushes the cached formatter for a specific locale.
    /// </summary>
    public void Flush(string locale)
    {
        _formatters.TryRemove(locale, out _);
    }
}
