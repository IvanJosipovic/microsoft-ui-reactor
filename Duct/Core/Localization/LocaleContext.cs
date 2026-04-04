namespace Duct.Core.Localization;

/// <summary>
/// Ambient context for the current locale. LocaleProvider sets this;
/// UseIntl() reads from it. Supports re-render subscriptions so components
/// are notified when the locale changes.
/// </summary>
internal sealed class LocaleContext
{
    /// <summary>
    /// The current (innermost) locale context. In a typical app there is
    /// one LocaleProvider at the root so this is effectively a singleton.
    /// </summary>
    internal static LocaleContext? Current { get; set; }

    private readonly List<Action> _subscribers = new();

    public IntlAccessor Accessor { get; private set; }

    internal LocaleContext(IntlAccessor accessor)
    {
        Accessor = accessor;
    }

    internal void UpdateAccessor(IntlAccessor accessor)
    {
        Accessor = accessor;
        NotifySubscribers();
    }

    internal void Subscribe(Action onLocaleChanged)
    {
        _subscribers.Add(onLocaleChanged);
    }

    internal void Unsubscribe(Action onLocaleChanged)
    {
        _subscribers.Remove(onLocaleChanged);
    }

    private void NotifySubscribers()
    {
        // Snapshot to avoid issues if subscriber list is modified during notification
        foreach (var subscriber in _subscribers.ToArray())
            subscriber();
    }
}
