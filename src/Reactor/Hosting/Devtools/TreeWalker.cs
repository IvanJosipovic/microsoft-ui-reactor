using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Reactor.Hosting.Devtools;

/// <summary>
/// Summary-view node shape emitted by <c>reactor.tree</c>. Full-view fields
/// (layout, visual, context) land in Phase 3 §3.1.
/// </summary>
internal sealed class TreeNode
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Name { get; set; }
    public string? AutomationId { get; set; }
    public string? AutomationName { get; set; }
    public BoundsBox Bounds { get; set; }
    public string? Text { get; set; }
    public bool IsVisible { get; set; }
    public string? ParentId { get; set; }
    public List<string> ChildIds { get; set; } = new();
    public object? Reactor { get; set; }
}

internal readonly record struct BoundsBox(double X, double Y, double Width, double Height);

/// <summary>
/// Walks a WinUI visual tree rooted at <c>Window.Content</c> and emits a flat
/// <see cref="TreeNode"/> array plus a pinned <c>$schema</c> tag. Must be called
/// on the UI dispatcher — <see cref="VisualTreeHelper.GetChild"/> requires it.
/// </summary>
internal sealed class TreeWalker
{
    public const string SchemaVersion = "reactor-tree/1";

    private readonly string _windowId;
    private readonly NodeRegistry _registry;

    public TreeWalker(string windowId, NodeRegistry registry)
    {
        _windowId = windowId;
        _registry = registry;
    }

    /// <summary>Walks the subtree rooted at <paramref name="root"/> and returns flat nodes.</summary>
    public List<TreeNode> Walk(UIElement? root)
    {
        var list = new List<TreeNode>();
        if (root is null) return list;
        WalkInto(root, parent: null, ancestor: null, siblingIndex: 0, list);
        return list;
    }

    private void WalkInto(
        UIElement element,
        NodeDescriptor? parent,
        NodeDescriptor? ancestor,
        int siblingIndex,
        List<TreeNode> sink)
    {
        var typeName = element.GetType().Name;
        var automationId = AutomationProperties.GetAutomationId(element);
        var automationName = AutomationProperties.GetName(element);
        var elementName = (element as FrameworkElement)?.Name;
        var componentName = InferComponentName(element) ?? parent?.ComponentName ?? typeName;

        var descriptor = new NodeDescriptor(
            WindowId: _windowId,
            ComponentName: componentName,
            AutomationId: string.IsNullOrEmpty(automationId) ? null : automationId,
            ReactorSource: null,
            TypeName: typeName,
            SiblingIndex: siblingIndex,
            StableAncestor: ancestor);

        var id = _registry.GetOrCreate(descriptor, element);

        var node = new TreeNode
        {
            Id = id,
            Type = typeName,
            Name = string.IsNullOrEmpty(elementName) ? null : elementName,
            AutomationId = string.IsNullOrEmpty(automationId) ? null : automationId,
            AutomationName = string.IsNullOrEmpty(automationName) ? null : automationName,
            Bounds = ReadBounds(element),
            Text = ExtractText(element),
            IsVisible = element.Visibility == Visibility.Visible,
            ParentId = parent is null ? null : NodeIdBuilder.Build(parent),
        };
        sink.Add(node);

        // Determine the next ancestor for content-addressed ids.
        bool isStable = !string.IsNullOrEmpty(descriptor.AutomationId) || descriptor.ReactorSource is not null;
        var nextAncestor = isStable ? descriptor : ancestor;

        int childCount = VisualTreeHelper.GetChildrenCount(element);
        for (int i = 0; i < childCount; i++)
        {
            if (VisualTreeHelper.GetChild(element, i) is UIElement child)
            {
                WalkInto(child, parent: descriptor, ancestor: nextAncestor, siblingIndex: i, sink);
                // Backfill the parent's childIds once we know the child id.
                var childDesc = new NodeDescriptor(
                    WindowId: _windowId,
                    ComponentName: InferComponentName(child) ?? componentName,
                    AutomationId: string.IsNullOrEmpty(AutomationProperties.GetAutomationId(child))
                        ? null
                        : AutomationProperties.GetAutomationId(child),
                    ReactorSource: null,
                    TypeName: child.GetType().Name,
                    SiblingIndex: i,
                    StableAncestor: nextAncestor);
                node.ChildIds.Add(NodeIdBuilder.Build(childDesc));
            }
        }
    }

    private static BoundsBox ReadBounds(UIElement element)
    {
        try
        {
            if (element is FrameworkElement fe)
                return new BoundsBox(0, 0, fe.ActualWidth, fe.ActualHeight);
        }
        catch { }
        return new BoundsBox(0, 0, 0, 0);
    }

    private static string? ExtractText(UIElement element) => element switch
    {
        TextBlock tb => tb.Text,
        TextBox tx => tx.Text,
        Button b => b.Content?.ToString(),
        ContentControl cc => cc.Content?.ToString(),
        _ => null,
    };

    /// <summary>
    /// Best-effort component inference. The root component instance is threaded
    /// into a window tag by the host (Phase 2.8+ wiring); without that tag the
    /// walker falls back to the element's type name so ids still work.
    /// </summary>
    private static string? InferComponentName(UIElement element)
    {
        // Reserved hook for source-map integration (Phase 3 §3.2). For now the
        // root component name is supplied by the caller through the initial
        // descriptor chain; nested components get their own name once the source
        // mapper lands.
        _ = element;
        return null;
    }
}

/// <summary>
/// Convenience payload with the pinned <c>$schema</c> tag alongside the flat
/// node array. The <c>$schema</c> property uses a literal JSON name — the
/// camelCase policy doesn't touch leading <c>$</c>.
/// </summary>
internal sealed class TreeResult
{
    [global::System.Text.Json.Serialization.JsonPropertyName("$schema")]
    public string Schema { get; set; } = TreeWalker.SchemaVersion;

    public List<TreeNode> Nodes { get; set; } = new();
    public string? WindowId { get; set; }
}
