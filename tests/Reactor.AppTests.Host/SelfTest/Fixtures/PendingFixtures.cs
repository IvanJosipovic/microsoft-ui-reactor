using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hooks;
using Microsoft.UI.Reactor.AppTests.Host.SelfTest;
using static Microsoft.UI.Reactor.Factories;
using static Microsoft.UI.Reactor.Hooks.PendingFactory;

namespace Microsoft.UI.Reactor.AppTests.Host.SelfTest.Fixtures;

/// <summary>
/// Selfhost coverage for the <c>Pending</c> bubble-up element. Unit tests exercise the
/// scope ref-count state machine in isolation; these fixtures verify the
/// <c>Visibility</c>-toggle contract on a real reconciled visual tree.
/// </summary>
internal static class PendingFixtures
{
    // ════════════════════════════════════════════════════════════════════
    //  BubbleUp — three nested components all fetching; fallback visible
    //  until every one resolves.
    // ════════════════════════════════════════════════════════════════════

    internal class BubbleUp(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var cache = new QueryCache();
            var g1 = new TaskCompletionSource<string>();
            var g2 = new TaskCompletionSource<string>();
            var g3 = new TaskCompletionSource<string>();

            var host = H.CreateHost();
            host.Mount(ctx => Pending(
                fallback: Factories.Text("⏳ pending-fallback"),
                child: VStack(
                    Factories.Component<ResourceChild, ResourceChildProps>(
                        new ResourceChildProps(cache, "bubble/a", g1.Task, "child-a")),
                    Factories.Component<ResourceChild, ResourceChildProps>(
                        new ResourceChildProps(cache, "bubble/b", g2.Task, "child-b")),
                    Factories.Component<ResourceChild, ResourceChildProps>(
                        new ResourceChildProps(cache, "bubble/c", g3.Task, "child-c"))
                )));

            await Harness.Render();

            // All three children are loading → fallback visible, children hidden.
            H.Check("Pending_BubbleUp_FallbackVisible",
                H.FindText("⏳ pending-fallback") is not null);
            // Children are mounted but their TextBlocks are hidden by Visibility.
            H.Check("Pending_BubbleUp_ChildAHidden", !IsTextVisible("child-a: data: a"));
            H.Check("Pending_BubbleUp_ChildBHidden", !IsTextVisible("child-b: data: b"));
            H.Check("Pending_BubbleUp_ChildCHidden", !IsTextVisible("child-c: data: c"));

            g1.SetResult("a");
            await Harness.Render();
            H.Check("Pending_BubbleUp_StillFallbackAfter1Resolves",
                IsTextVisible("⏳ pending-fallback"));

            g2.SetResult("b");
            await Harness.Render();
            H.Check("Pending_BubbleUp_StillFallbackAfter2Resolve",
                IsTextVisible("⏳ pending-fallback"));

            g3.SetResult("c");
            await Harness.Render();

            H.Check("Pending_BubbleUp_ChildrenVisibleWhenAllResolved",
                IsTextVisible("child-a: data: a") &&
                IsTextVisible("child-b: data: b") &&
                IsTextVisible("child-c: data: c"));

            H.Check("Pending_BubbleUp_FallbackHidden",
                !IsTextVisible("⏳ pending-fallback"));
        }

        bool IsTextVisible(string text)
        {
            var tb = H.FindText(text);
            if (tb is null) return false;
            // Walk up the visual tree; if any ancestor is Collapsed, the element is hidden.
            Microsoft.UI.Xaml.DependencyObject? cur = tb;
            while (cur is not null)
            {
                if (cur is Microsoft.UI.Xaml.UIElement ui &&
                    ui.Visibility == Microsoft.UI.Xaml.Visibility.Collapsed)
                    return false;
                cur = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(cur);
            }
            return true;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  WithOverride — a child renders its own placeholder locally, outer
    //  Pending still waits for that child's resource before hiding the
    //  outer fallback. Child-local handling does not "claim" the scope.
    // ════════════════════════════════════════════════════════════════════

    internal class WithOverride(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var cache = new QueryCache();
            var gate = new TaskCompletionSource<string>();

            var host = H.CreateHost();
            host.Mount(ctx => Pending(
                fallback: Factories.Text("⏳ outer-fallback"),
                child: Factories.Component<LocalMatchingChild, LocalMatchingChildProps>(
                    new LocalMatchingChildProps(cache, "override/key", gate.Task))));

            await Harness.Render();

            // Child is Loading — outer Pending fallback is visible.
            H.Check("Pending_Override_OuterFallbackVisible",
                H.FindText("⏳ outer-fallback") is not null);

            gate.SetResult("value!");
            await Harness.Render();

            // Once the resource lands, the child renders its Data branch.
            H.Check("Pending_Override_ChildDataVisible",
                H.FindText("local: value!") is not null);
            H.Check("Pending_Override_OuterFallbackHidden",
                H.FindText("⏳ outer-fallback") is null ||
                !IsEffectivelyVisible(H.FindText("⏳ outer-fallback")!));
        }

        static bool IsEffectivelyVisible(Microsoft.UI.Xaml.Controls.TextBlock tb)
        {
            Microsoft.UI.Xaml.DependencyObject? cur = tb;
            while (cur is not null)
            {
                if (cur is Microsoft.UI.Xaml.UIElement ui &&
                    ui.Visibility == Microsoft.UI.Xaml.Visibility.Collapsed)
                    return false;
                cur = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(cur);
            }
            return true;
        }
    }

    // ─── Support components ─────────────────────────────────────────────

    internal sealed record ResourceChildProps(QueryCache Cache, string Key, Task<string> Gate, string Label);

    internal sealed class ResourceChild : Component<ResourceChildProps>
    {
        public override Element Render()
        {
            var p = Props;
            var v = UseResource(
                _ => p.Gate,
                p.Cache,
                Array.Empty<object>(),
                new ResourceOptions(CacheKey: p.Key));
            return Factories.Text($"{p.Label}: {v switch
            {
                AsyncValue<string>.Loading => "loading",
                AsyncValue<string>.Data d => $"data: {d.Value}",
                AsyncValue<string>.Reloading r => $"reloading: {r.Previous}",
                AsyncValue<string>.Error e => $"error: {e.Exception.Message}",
                _ => "?",
            }}");
        }
    }

    internal sealed record LocalMatchingChildProps(QueryCache Cache, string Key, Task<string> Gate);

    /// <summary>
    /// Reads AsyncValue and renders its own local placeholder for Loading. This exercises
    /// the spec §10 override scenario: a local match does not suppress the bubble-up —
    /// the hook still registers as Loading with the scope until resolved.
    /// </summary>
    internal sealed class LocalMatchingChild : Component<LocalMatchingChildProps>
    {
        public override Element Render()
        {
            var p = Props;
            var v = UseResource(
                _ => p.Gate,
                p.Cache,
                Array.Empty<object>(),
                new ResourceOptions(CacheKey: p.Key));

            return v switch
            {
                AsyncValue<string>.Loading => Factories.Text("(local skeleton)"),
                AsyncValue<string>.Data d => Factories.Text($"local: {d.Value}"),
                AsyncValue<string>.Reloading r => Factories.Text($"local: {r.Previous}"),
                AsyncValue<string>.Error e => Factories.Text($"local-error: {e.Exception.Message}"),
                _ => Factories.Text("?"),
            };
        }
    }
}
