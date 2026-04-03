using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace Duct.Core;

/// <summary>
/// Manages recursive INPC subscriptions for a single UseObservableTree call.
/// Walks the object graph from a root, subscribes to PropertyChanged on every
/// reachable INotifyPropertyChanged object, and re-renders on any change.
/// Automatically handles cycle detection, nested object replacement, and cleanup.
/// </summary>
internal class ObservableTreeTracker : IDisposable
{
    private readonly Action _requestRerender;
    private readonly Dictionary<INotifyPropertyChanged, PropertyChangedEventHandler> _subscriptions = new();
    private readonly HashSet<INotifyPropertyChanged> _visiting = new();

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _inpcPropertyCache = new();

    public ObservableTreeTracker(Action requestRerender)
        => _requestRerender = requestRerender;

    /// <summary>
    /// Per-type cache of properties that could hold INPC values.
    /// Filters to: public instance properties, getter accessible,
    /// property type is class or interface (value types can't be INPC).
    /// </summary>
    internal static PropertyInfo[] GetInpcCandidateProperties(Type type)
        => _inpcPropertyCache.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
             .Where(p => p.CanRead && !p.PropertyType.IsValueType)
             .ToArray());

    /// <summary>
    /// Synchronize subscriptions to match the current object graph.
    /// Called on mount and whenever the source reference changes.
    /// </summary>
    public void SyncSubscriptions(INotifyPropertyChanged root)
    {
        var desiredSet = new HashSet<INotifyPropertyChanged>(ReferenceEqualityComparer.Instance);
        _visiting.Clear();
        Walk(root, desiredSet);

        // Unsubscribe from objects no longer in the graph
        var toRemove = new List<INotifyPropertyChanged>();
        foreach (var kvp in _subscriptions)
        {
            if (!desiredSet.Contains(kvp.Key))
            {
                kvp.Key.PropertyChanged -= kvp.Value;
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var obj in toRemove)
            _subscriptions.Remove(obj);

        // Subscribe to new objects in the graph
        foreach (var obj in desiredSet)
        {
            if (!_subscriptions.ContainsKey(obj))
            {
                PropertyChangedEventHandler handler = OnNestedPropertyChanged;
                obj.PropertyChanged += handler;
                _subscriptions[obj] = handler;
            }
        }
    }

    public void Dispose()
    {
        foreach (var kvp in _subscriptions)
            kvp.Key.PropertyChanged -= kvp.Value;
        _subscriptions.Clear();
    }

    private void Walk(INotifyPropertyChanged? node, HashSet<INotifyPropertyChanged> desiredSet)
    {
        if (node is null || !_visiting.Add(node))
            return; // null or cycle detected

        desiredSet.Add(node);

        foreach (var prop in GetInpcCandidateProperties(node.GetType()))
        {
            try
            {
                var value = prop.GetValue(node);
                if (value is INotifyPropertyChanged inpc)
                    Walk(inpc, desiredSet);
            }
            catch
            {
                // Skip properties that throw on access
            }
        }

        _visiting.Remove(node);
    }

    private void OnNestedPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _requestRerender();

        if (sender is null || string.IsNullOrEmpty(e.PropertyName))
            return;

        var senderType = sender.GetType();
        var prop = senderType.GetProperty(e.PropertyName, BindingFlags.Public | BindingFlags.Instance);
        if (prop is null || prop.PropertyType.IsValueType)
            return;

        try
        {
            var newValue = prop.GetValue(sender);

            // Unsubscribe from old subtree if it was INPC
            // We can't easily track the old value, so re-sync is the safe approach.
            // However, for efficiency we do a targeted re-sync: just rebuild the desired set
            // and reconcile. This handles both old-unsubscribe and new-subscribe.
            // Find any root we can walk from — we need to re-sync from the whole graph.
            // The simplest correct approach: rebuild the entire subscription set.
            // This is O(N) where N = INPC objects in graph, but only on property changes
            // where the property type could be INPC.
            var root = FindRoot();
            if (root is not null)
                SyncSubscriptions(root);
        }
        catch
        {
            // Property access failed — skip subtree update
        }
    }

    private INotifyPropertyChanged? FindRoot()
    {
        // The first subscription added is always the root (from SyncSubscriptions).
        // Since Dictionary preserves insertion order in .NET, the first key is the root.
        foreach (var kvp in _subscriptions)
            return kvp.Key;
        return null;
    }
}
