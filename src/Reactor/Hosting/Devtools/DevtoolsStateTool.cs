using System.Collections;
using System.Text.Json;
using Microsoft.UI.Reactor.Core;

namespace Microsoft.UI.Reactor.Hosting.Devtools;

/// <summary>
/// <c>reactor.state</c> — read-only inspection of the reactive state attached to
/// the root component's <see cref="RenderContext"/>. Primitives serialize as
/// JSON values; complex objects return <c>{ $type, $shape }</c> per spec §12.
/// Mutation is out of scope — agents mutate via <c>reactor.fire</c> or by
/// editing source and calling <c>reactor.reload</c>.
///
/// Scope: v1 walks the root component only (matches <c>reactor.fire</c>). Child
/// components join once the reconciler exposes a component registry.
/// </summary>
internal static class DevtoolsStateTool
{
    public static void Register(DevtoolsMcpServer server, Func<Component?> rootComponent)
    {
        server.Tools.Register(
            new McpToolDescriptor(
                Name: "state",
                Description: "Reads reactive state from the root component's hook table. Primitives return as JSON; complex objects return as { $type, $shape }.",
                InputSchema: new
                {
                    type = "object",
                    properties = new
                    {
                        selector = new { type = "string", description = "Reserved — v1 always inspects the root component." },
                    },
                    additionalProperties = false,
                }),
            @params => server.OnDispatcher<object>(() => BuildPayload(rootComponent())));
    }

    /// <summary>
    /// Core state-read. Public to the test project so shape assertions don't
    /// need to stand up an HTTP listener. Throws <see cref="McpToolException"/>
    /// on the not-ready path so the live handler path sees the same shape.
    /// </summary>
    internal static object BuildPayload(Component? root)
    {
        if (root is null)
            throw new McpToolException(
                "No root component is mounted.",
                JsonRpcErrorCodes.ToolExecution,
                new { code = "not-ready" });

        var ctx = GetContext(root);
        var snapshots = ctx.SnapshotHooks();
        var componentName = root.GetType().Name;
        // Instance id mirrors the fixture-root id from the tree walker —
        // main window, component class, root marker. Avoids forcing the
        // agent to resolve it separately.
        var instanceId = $"r:main/{componentName}.root";

        var hooks = snapshots.Select(s => new
        {
            component = componentName,
            instanceId,
            hook = s.Hook,
            index = s.Index,
            valueType = s.ValueType?.FullName ?? s.ValueType?.Name,
            value = ShapeValue(s.Value),
        }).ToArray();

        return new { hooks };
    }

    /// <summary>
    /// Reads the internal <see cref="RenderContext"/> of a class component. The
    /// property is <c>internal</c> so we resolve it once reflectively here —
    /// avoids adding a public accessor just for devtools.
    /// </summary>
    private static RenderContext GetContext(Component c)
    {
        var prop = typeof(Component).GetProperty(
            "Context",
            global::System.Reflection.BindingFlags.Instance |
            global::System.Reflection.BindingFlags.NonPublic)!;
        return (RenderContext)prop.GetValue(c)!;
    }

    /// <summary>
    /// Shapes a hook value for JSON transport. Primitives pass through; strings
    /// pass through; everything else becomes <c>{ $type, $shape }</c>. Null is
    /// returned as-is (JsonSerializer writes <c>null</c>).
    /// </summary>
    internal static object? ShapeValue(object? value)
    {
        if (value is null) return null;

        // Primitives + string + decimal ship as literals.
        if (value is string or bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal)
            return value;

        // Enums present as their string form — easier to read than the raw int.
        if (value.GetType().IsEnum) return value.ToString();

        // Collections: emit the count only. A full dump is a privacy/serialization pit per §12.
        if (value is IEnumerable enumerable and not string)
        {
            int count = 0;
            foreach (var _ in enumerable) count++;
            return new Dictionary<string, object?>
            {
                ["$type"] = value.GetType().FullName ?? value.GetType().Name,
                ["$shape"] = new Dictionary<string, object?>
                {
                    ["kind"] = "collection",
                    ["count"] = count,
                },
            };
        }

        // Complex object: return the public property shape (names + type names),
        // not values. Values would reopen the DataContext-dump problem the spec
        // explicitly rejects.
        var type = value.GetType();
        var props = type.GetProperties(
            global::System.Reflection.BindingFlags.Instance |
            global::System.Reflection.BindingFlags.Public);
        var shape = new Dictionary<string, object?>();
        foreach (var p in props)
        {
            shape[p.Name] = p.PropertyType.Name;
        }
        return new Dictionary<string, object?>
        {
            ["$type"] = type.FullName ?? type.Name,
            ["$shape"] = shape,
        };
    }
}
