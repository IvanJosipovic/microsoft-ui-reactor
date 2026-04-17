using Microsoft.UI.Reactor.Localization;

namespace Microsoft.UI.Reactor.Tests.Localization;

/// <summary>
/// In-memory resource provider for testing without real .resw files.
/// Stores strings keyed by (locale, namespace, key).
/// </summary>
internal sealed class InMemoryResourceProvider : IStringResourceProvider
{
    private readonly Dictionary<(string Locale, string Namespace, string Key), string> _strings = new(
        new LocaleKeyComparer());

    public InMemoryResourceProvider Add(string locale, string ns, string key, string value)
    {
        _strings[(locale, ns, key)] = value;
        return this;
    }

    public string? GetString(string locale, string ns, string key)
    {
        return _strings.TryGetValue((locale, ns, key), out var value) ? value : null;
    }

    private sealed class LocaleKeyComparer : IEqualityComparer<(string Locale, string Namespace, string Key)>
    {
        public bool Equals((string Locale, string Namespace, string Key) x,
                           (string Locale, string Namespace, string Key) y) =>
            StringComparer.OrdinalIgnoreCase.Equals(x.Locale, y.Locale) &&
            StringComparer.OrdinalIgnoreCase.Equals(x.Namespace, y.Namespace) &&
            StringComparer.Ordinal.Equals(x.Key, y.Key);

        public int GetHashCode((string Locale, string Namespace, string Key) obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Locale),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Namespace),
                StringComparer.Ordinal.GetHashCode(obj.Key));
    }
}
