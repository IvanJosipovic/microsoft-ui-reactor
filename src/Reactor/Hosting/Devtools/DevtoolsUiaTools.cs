using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Reactor.Hosting.Devtools;

/// <summary>
/// Registers the UIA-driven automation tools: <c>tree</c>, <c>click</c>, <c>type</c>,
/// <c>focus</c>, <c>waitFor</c>. Each handler marshals onto the UI dispatcher and
/// surfaces structured errors via <see cref="McpToolException"/>.
/// </summary>
internal static class DevtoolsUiaTools
{
    public static void RegisterUiaTools(
        DevtoolsMcpServer server,
        NodeRegistry nodes,
        WindowRegistry windows)
    {
        var resolver = new SelectorResolver(nodes, windows);

        Register_Tree(server, nodes, windows);
        Register_Click(server, resolver);
        Register_Type(server, resolver);
        Register_Focus(server, resolver);
        Register_WaitFor(server, resolver);
        Register_Screenshot(server, resolver, windows);
    }

    // -- screenshot --------------------------------------------------------------

    private static void Register_Screenshot(DevtoolsMcpServer server, SelectorResolver resolver, WindowRegistry windows)
    {
        server.Tools.Register(
            new McpToolDescriptor(
                Name: "screenshot",
                Description: "Captures a PNG of the window (or a selector-scoped region). Base64-encoded result.",
                InputSchema: new
                {
                    type = "object",
                    properties = new
                    {
                        selector = new { type = "string", description = "Optional region to crop to." },
                        window = new { type = "string" },
                        waitIdle = new { type = "boolean", description = "Force a layout pass before capture (default true)." },
                        includeChrome = new { type = "boolean", description = "Include the non-client titlebar frame (default false)." },
                    },
                    additionalProperties = false,
                }),
            @params => server.OnDispatcher(() =>
            {
                var selector = DevtoolsTools.ReadString(@params, "selector");
                var windowId = DevtoolsTools.ReadString(@params, "window");
                var waitIdle = DevtoolsTools.ReadBool(@params, "waitIdle") ?? true;
                var includeChrome = DevtoolsTools.ReadBool(@params, "includeChrome") ?? false;

                var w = ResolveWindowForTools(windows, windowId);

                (double x, double y, double w, double h)? crop = null;
                if (!string.IsNullOrEmpty(selector))
                {
                    var el = resolver.Resolve(selector!, windowId);
                    if (el is FrameworkElement fe)
                    {
                        // Element bounds relative to the window client area.
                        var transform = fe.TransformToVisual(w.Content);
                        var origin = transform.TransformPoint(new global::Windows.Foundation.Point(0, 0));
                        crop = (origin.X, origin.Y, fe.ActualWidth, fe.ActualHeight);
                    }
                }

                if (waitIdle)
                {
                    // A no-op UpdateLayout on the content tree forces pending layout
                    // to run before we capture.
                    if (w.Content is FrameworkElement rootFe) rootFe.UpdateLayout();
                }

                var capture = ScreenshotCapture.CaptureWindow(w, includeChrome, crop);
                return new
                {
                    png = Convert.ToBase64String(capture.Png),
                    bounds = new
                    {
                        x = capture.X,
                        y = capture.Y,
                        width = capture.Width,
                        height = capture.Height,
                    },
                };
            }, timeoutMs: 10_000));
    }

    // -- tree --------------------------------------------------------------------

    private static void Register_Tree(DevtoolsMcpServer server, NodeRegistry nodes, WindowRegistry windows)
    {
        server.Tools.Register(
            new McpToolDescriptor(
                Name: "tree",
                Description: "Walks the visual tree and returns a flat summary-view array of nodes.",
                InputSchema: new
                {
                    type = "object",
                    properties = new
                    {
                        selector = new { type = "string", description = "Optional scope; if omitted, walks the whole window." },
                        window = new { type = "string" },
                        view = new { type = "string", @enum = new[] { "summary" } },
                        includeReactorSource = new { type = "boolean" },
                    },
                    additionalProperties = false,
                }),
            @params => server.OnDispatcher(() =>
            {
                var selector = DevtoolsTools.ReadString(@params, "selector");
                var windowId = DevtoolsTools.ReadString(@params, "window");
                var w = ResolveWindowForTools(windows, windowId);
                var walker = new TreeWalker(WindowIdFor(windows, w), nodes);

                UIElement? root = w.Content;
                if (!string.IsNullOrEmpty(selector))
                {
                    var resolver = new SelectorResolver(nodes, windows);
                    root = resolver.Resolve(selector!, windowId);
                }

                return new TreeResult
                {
                    Schema = TreeWalker.SchemaVersion,
                    WindowId = WindowIdFor(windows, w),
                    Nodes = walker.Walk(root),
                };
            }));
    }

    // -- click -------------------------------------------------------------------

    private static void Register_Click(DevtoolsMcpServer server, SelectorResolver resolver)
    {
        server.Tools.Register(
            new McpToolDescriptor(
                Name: "click",
                Description: "Clicks the element matching the selector. Prefers IInvokeProvider, falls back to Toggle → SelectionItem → pointer.",
                InputSchema: new
                {
                    type = "object",
                    properties = new
                    {
                        selector = new { type = "string" },
                        window = new { type = "string" },
                    },
                    required = new[] { "selector" },
                    additionalProperties = false,
                }),
            @params => server.OnDispatcher(() =>
            {
                var selector = RequiredString(@params, "selector");
                var windowId = DevtoolsTools.ReadString(@params, "window");
                var el = resolver.Resolve(selector, windowId);
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(el)
                    ?? throw new McpToolException(
                        "Element has no automation peer.",
                        JsonRpcErrorCodes.ToolExecution,
                        new { code = "no-peer" });

                if (peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider invoke)
                {
                    invoke.Invoke();
                    return new { ok = true, via = "invoke" };
                }
                if (peer.GetPattern(PatternInterface.Toggle) is IToggleProvider toggle)
                {
                    toggle.Toggle();
                    return new { ok = true, via = "toggle" };
                }
                if (peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider sel)
                {
                    sel.Select();
                    return new { ok = true, via = "selection" };
                }

                throw new McpToolException(
                    "No UIA pattern available to click this element.",
                    JsonRpcErrorCodes.ToolExecution,
                    new { code = "no-pattern" });
            }));
    }

    // -- type --------------------------------------------------------------------

    private static void Register_Type(DevtoolsMcpServer server, SelectorResolver resolver)
    {
        server.Tools.Register(
            new McpToolDescriptor(
                Name: "type",
                Description: "Sets text on a value-bearing control. Clears first when clear=true.",
                InputSchema: new
                {
                    type = "object",
                    properties = new
                    {
                        selector = new { type = "string" },
                        text = new { type = "string" },
                        clear = new { type = "boolean" },
                        window = new { type = "string" },
                    },
                    required = new[] { "selector", "text" },
                    additionalProperties = false,
                }),
            @params => server.OnDispatcher(() =>
            {
                var selector = RequiredString(@params, "selector");
                var text = RequiredString(@params, "text");
                var clear = DevtoolsTools.ReadBool(@params, "clear") ?? false;
                var windowId = DevtoolsTools.ReadString(@params, "window");

                var el = resolver.Resolve(selector, windowId);

                if (el is TextBox tb)
                {
                    tb.Text = clear ? text : tb.Text + text;
                    return new { ok = true, via = "value" };
                }

                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(el);
                if (peer?.GetPattern(PatternInterface.Value) is IValueProvider value)
                {
                    var current = value.Value ?? string.Empty;
                    value.SetValue(clear ? text : current + text);
                    return new { ok = true, via = "value" };
                }

                throw new McpToolException(
                    "Element does not expose a value pattern.",
                    JsonRpcErrorCodes.ToolExecution,
                    new { code = "no-pattern" });
            }));
    }

    // -- focus -------------------------------------------------------------------

    private static void Register_Focus(DevtoolsMcpServer server, SelectorResolver resolver)
    {
        server.Tools.Register(
            new McpToolDescriptor(
                Name: "focus",
                Description: "Programmatically focuses the selected element.",
                InputSchema: new
                {
                    type = "object",
                    properties = new
                    {
                        selector = new { type = "string" },
                        window = new { type = "string" },
                    },
                    required = new[] { "selector" },
                    additionalProperties = false,
                }),
            @params => server.OnDispatcher(() =>
            {
                var selector = RequiredString(@params, "selector");
                var windowId = DevtoolsTools.ReadString(@params, "window");
                var el = resolver.Resolve(selector, windowId);
                bool focused = false;
                if (el is Control ctl) focused = ctl.Focus(FocusState.Programmatic);
                return new { ok = focused };
            }));
    }

    // -- waitFor -----------------------------------------------------------------

    private static void Register_WaitFor(DevtoolsMcpServer server, SelectorResolver resolver)
    {
        server.Tools.Register(
            new McpToolDescriptor(
                Name: "waitFor",
                Description: "Polls a predicate against the live tree until it matches or times out.",
                InputSchema: new
                {
                    type = "object",
                    properties = new
                    {
                        predicate = new
                        {
                            type = "object",
                            properties = new
                            {
                                selector = new { type = "string" },
                                textEquals = new { type = "string" },
                                textMatches = new { type = "string" },
                                visible = new { type = "boolean" },
                                count = new { type = "integer" },
                            },
                        },
                        timeoutMs = new { type = "integer" },
                        window = new { type = "string" },
                    },
                    required = new[] { "predicate" },
                    additionalProperties = false,
                }),
            @params =>
            {
                if (@params is not { } p || p.ValueKind != JsonValueKind.Object)
                    throw new McpToolException("waitFor requires a 'predicate' object.", JsonRpcErrorCodes.InvalidParams);
                if (!p.TryGetProperty("predicate", out var predEl) || predEl.ValueKind != JsonValueKind.Object)
                    throw new McpToolException("'predicate' must be an object.", JsonRpcErrorCodes.InvalidParams);

                var pred = WaitForPredicate.FromJson(predEl);
                int timeoutMs = DevtoolsTools.ReadInt(@params, "timeoutMs") ?? 5000;
                var windowId = DevtoolsTools.ReadString(@params, "window");

                var sw = global::System.Diagnostics.Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < timeoutMs)
                {
                    var observed = server.OnDispatcher(() => WaitForPredicate.Evaluate(pred, resolver, windowId));
                    if (observed.Satisfied)
                        return new { ok = true, elapsedMs = sw.ElapsedMilliseconds };
                    Thread.Sleep(50);
                }

                var final = server.OnDispatcher(() => WaitForPredicate.Evaluate(pred, resolver, windowId));
                return new
                {
                    ok = false,
                    reason = "timeout",
                    elapsedMs = sw.ElapsedMilliseconds,
                    observed = new
                    {
                        count = final.Count,
                        text = final.Text,
                        visible = final.Visible,
                    },
                };
            });
    }

    // -- helpers -----------------------------------------------------------------

    private static string RequiredString(JsonElement? args, string key) =>
        DevtoolsTools.ReadString(args, key)
            ?? throw new McpToolException($"Missing required argument '{key}'.",
                JsonRpcErrorCodes.InvalidParams);

    private static Window ResolveWindowForTools(WindowRegistry windows, string? explicitWindowId)
    {
        if (!string.IsNullOrEmpty(explicitWindowId))
        {
            var w = windows.Resolve(explicitWindowId!);
            return w ?? throw new McpToolException(
                $"Window '{explicitWindowId}' not found.", JsonRpcErrorCodes.ToolExecution,
                new { code = "unknown-window" });
        }
        var @default = windows.TryDefault(out var activeIds);
        if (@default is not null) return @default;
        throw new McpToolException(
            "Multiple windows are active — pass 'window'.",
            JsonRpcErrorCodes.InvalidParams,
            new { code = "window-required", activeIds });
    }

    private static string WindowIdFor(WindowRegistry windows, Window w)
    {
        foreach (var snap in windows.Snapshot())
        {
            if (windows.Resolve(snap.Id) == w) return snap.Id;
        }
        return "main";
    }
}

/// <summary>Predicate IR + evaluator for <c>reactor.waitFor</c>.</summary>
internal sealed record WaitForPredicate(
    string? Selector,
    string? TextEquals,
    string? TextMatches,
    bool? Visible,
    int? Count)
{
    public static WaitForPredicate FromJson(JsonElement el) => new(
        Selector: el.TryGetProperty("selector", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString() : null,
        TextEquals: el.TryGetProperty("textEquals", out var te) && te.ValueKind == JsonValueKind.String ? te.GetString() : null,
        TextMatches: el.TryGetProperty("textMatches", out var tm) && tm.ValueKind == JsonValueKind.String ? tm.GetString() : null,
        Visible: el.TryGetProperty("visible", out var v) && v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : null,
        Count: el.TryGetProperty("count", out var c) && c.ValueKind == JsonValueKind.Number && c.TryGetInt32(out var ci) ? ci : null);

    public readonly record struct Observation(bool Satisfied, int Count, string? Text, bool Visible);

    public static Observation Evaluate(WaitForPredicate pred, SelectorResolver resolver, string? windowId)
    {
        if (string.IsNullOrEmpty(pred.Selector))
            return new Observation(Satisfied: true, Count: 0, Text: null, Visible: false);

        // Count-first: count=0 means "wait for element to disappear", so we
        // must allow resolution failure to succeed in that case.
        try
        {
            var element = resolver.Resolve(pred.Selector!, windowId);
            var text = ExtractText(element);
            var visible = element is UIElement u && u.Visibility == Visibility.Visible;

            bool ok = true;
            if (pred.Count is int c && c != 1) ok = false; // a single Resolve() match means count=1
            if (pred.TextEquals is not null && pred.TextEquals != text) ok = false;
            if (pred.TextMatches is not null)
            {
                try { if (!Regex.IsMatch(text ?? string.Empty, pred.TextMatches)) ok = false; }
                catch (RegexParseException) { ok = false; }
            }
            if (pred.Visible is bool vb && vb != visible) ok = false;

            return new Observation(ok, Count: 1, Text: text, Visible: visible);
        }
        catch (McpToolException ex) when (IsUnknownSelector(ex))
        {
            // Element absent — only satisfies count==0.
            bool ok = pred.Count == 0;
            return new Observation(ok, Count: 0, Text: null, Visible: false);
        }
    }

    private static bool IsUnknownSelector(McpToolException ex)
    {
        if (ex.Payload is null) return false;
        var json = JsonSerializer.Serialize(ex.Payload);
        return json.Contains("unknown-selector");
    }

    private static string? ExtractText(UIElement element) => element switch
    {
        TextBlock tb => tb.Text,
        TextBox tx => tx.Text,
        Button b => b.Content?.ToString(),
        ContentControl cc => cc.Content?.ToString(),
        _ => null,
    };
}
