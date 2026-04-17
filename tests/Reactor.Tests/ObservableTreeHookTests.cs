using System.ComponentModel;
using Microsoft.UI.Reactor.Core;
using Xunit;

namespace Microsoft.UI.Reactor.Tests;

/// <summary>
/// Tests for UseObservableTree — deep/recursive INPC observation.
/// </summary>
public class ObservableTreeHookTests
{
    // ── Test models ───────────────────────────────────────────────

    private class ChildModel : INotifyPropertyChanged
    {
        private string _value = "";
        public string Value
        {
            get => _value;
            set { if (_value != value) { _value = value; OnPropertyChanged(nameof(Value)); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private class ParentModel : INotifyPropertyChanged
    {
        private ChildModel _child = new();
        public ChildModel Child
        {
            get => _child;
            set { if (_child != value) { _child = value; OnPropertyChanged(nameof(Child)); } }
        }

        private string _label = "";
        public string Label
        {
            get => _label;
            set { if (_label != value) { _label = value; OnPropertyChanged(nameof(Label)); } }
        }

        // Non-INPC reference property — should be ignored, not crash
        public string? Tag { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private class GrandparentModel : INotifyPropertyChanged
    {
        private ParentModel _parent = new();
        public ParentModel Parent
        {
            get => _parent;
            set { if (_parent != value) { _parent = value; OnPropertyChanged(nameof(Parent)); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private class CircularNode : INotifyPropertyChanged
    {
        private CircularNode? _other;
        public CircularNode? Other
        {
            get => _other;
            set { if (_other != value) { _other = value; OnPropertyChanged(nameof(Other)); } }
        }

        private string _name = "";
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ── Helper ────────────────────────────────────────────────────

    private static void Render(RenderContext ctx, int[] counter, INotifyPropertyChanged source)
    {
        ctx.BeginRender(() => counter[0]++);
        ctx.UseObservableTree(source);
        ctx.FlushEffects();
    }

    // ── Tests ─────────────────────────────────────────────────────

    [Fact]
    public void Nested_Property_Change_Triggers_Rerender()
    {
        var ctx = new RenderContext();
        var parent = new ParentModel { Child = new ChildModel { Value = "old" } };
        var c = new[] { 0 };

        Render(ctx, c, parent);
        Render(ctx, c, parent);

        parent.Child.Value = "new";
        Assert.True(c[0] >= 1, $"Expected rerender, got {c[0]}");
    }

    [Fact]
    public void Circular_References_Do_Not_Infinite_Loop()
    {
        var ctx = new RenderContext();
        var nodeA = new CircularNode { Name = "A" };
        var nodeB = new CircularNode { Name = "B" };
        nodeA.Other = nodeB;
        nodeB.Other = nodeA;
        var c = new[] { 0 };

        // Should not throw or hang
        Render(ctx, c, nodeA);
        Render(ctx, c, nodeA);

        nodeA.Name = "A2";
        Assert.True(c[0] >= 1);

        Render(ctx, c, nodeA);
        int before = c[0];
        nodeB.Name = "B2";
        Assert.True(c[0] > before);
    }

    [Fact]
    public void Replaced_Nested_INPC_Causes_Resubscribe()
    {
        var ctx = new RenderContext();
        var oldChild = new ChildModel { Value = "old" };
        var newChild = new ChildModel { Value = "new" };
        var parent = new ParentModel { Child = oldChild };
        var c = new[] { 0 };

        Render(ctx, c, parent);
        Render(ctx, c, parent);

        // Replace child — triggers re-sync in OnNestedPropertyChanged
        parent.Child = newChild;

        // Re-render to pick up new subscriptions
        Render(ctx, c, parent);

        int before = c[0];
        oldChild.Value = "changed";
        Assert.Equal(before, c[0]); // old child unsubscribed

        newChild.Value = "updated";
        Assert.True(c[0] > before); // new child subscribed
    }

    [Fact]
    public void Disposal_Cleans_All_Subscriptions()
    {
        var ctx = new RenderContext();
        var child = new ChildModel { Value = "v" };
        var parent = new ParentModel { Child = child };
        var c = new[] { 0 };

        Render(ctx, c, parent);
        Render(ctx, c, parent);

        ctx.RunCleanups();

        parent.Label = "changed";
        Assert.Equal(0, c[0]);

        child.Value = "changed";
        Assert.Equal(0, c[0]);
    }

    [Fact]
    public void Source_Reference_Change_Disposes_Old_And_Creates_New()
    {
        var ctx = new RenderContext();
        var source1 = new ParentModel { Child = new ChildModel { Value = "1" } };
        var source2 = new ParentModel { Child = new ChildModel { Value = "2" } };
        var c = new[] { 0 };

        Render(ctx, c, source1);

        // Switch to source2
        ctx.BeginRender(() => c[0]++);
        ctx.UseObservableTree(source2);
        ctx.FlushEffects();

        source1.Child.Value = "changed1";
        Assert.Equal(0, c[0]);

        source2.Child.Value = "changed2";
        Assert.True(c[0] >= 1);
    }

    [Fact]
    public void Non_INPC_Nested_Properties_Are_Ignored()
    {
        var ctx = new RenderContext();
        var parent = new ParentModel
        {
            Child = new ChildModel { Value = "v" },
            Tag = "some-tag"
        };
        var c = new[] { 0 };

        // Should not throw
        Render(ctx, c, parent);
        Render(ctx, c, parent);

        parent.Child.Value = "new";
        Assert.True(c[0] >= 1);
    }

    [Fact]
    public void Direct_Property_Change_On_Root_Also_Triggers_Rerender()
    {
        var ctx = new RenderContext();
        var parent = new ParentModel { Label = "original" };
        var c = new[] { 0 };

        Render(ctx, c, parent);
        Render(ctx, c, parent);

        parent.Label = "changed";
        Assert.True(c[0] >= 1);
    }

    [Fact]
    public void Deep_Nesting_Three_Levels()
    {
        var ctx = new RenderContext();
        var gp = new GrandparentModel
        {
            Parent = new ParentModel
            {
                Child = new ChildModel { Value = "deep" }
            }
        };
        var c = new[] { 0 };

        Render(ctx, c, gp);
        Render(ctx, c, gp);

        gp.Parent.Child.Value = "deeper";
        Assert.True(c[0] >= 1);
    }
}
