using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml.Controls;
using Xunit;

namespace Microsoft.UI.Reactor.Tests;

/// <summary>
/// Tests for ReactorHost and ReactorHostControl API surface and contracts.
/// Runtime lifecycle tests (mounting, rendering, state changes) require a XAML
/// Application context and UI thread — those are covered by the Reactor.TestApp integration tests.
/// </summary>
public class ReactorHostRenderLoopTests
{
    // ── ReactorHostControl API surface ──────────────────────────────

    [Fact]
    public void HostControl_Constructor_Accepts_Logger()
    {
        var ctor = typeof(ReactorHostControl).GetConstructor([typeof(Component), typeof(ILogger)]);
        Assert.NotNull(ctor);
        // Both parameters should be optional
        foreach (var param in ctor!.GetParameters())
            Assert.True(param.HasDefaultValue, $"Parameter '{param.Name}' should have a default value");
    }

    [Fact]
    public void HostControl_Has_Default_Constructor()
    {
        // Constructor has (Component?, ILogger?) — both optional, so callable with no args
        var ctor = typeof(ReactorHostControl).GetConstructor([typeof(Component), typeof(ILogger)]);
        Assert.NotNull(ctor);
        Assert.True(ctor!.GetParameters().All(p => p.HasDefaultValue));
    }

    // ── ReactorHost API surface ─────────────────────────────────────

    [Fact]
    public void Host_Has_Mount_Component_Method()
    {
        var method = typeof(ReactorHost).GetMethod("Mount", [typeof(Component)]);
        Assert.NotNull(method);
    }

    [Fact]
    public void Host_Has_Mount_Func_Method()
    {
        var method = typeof(ReactorHost).GetMethod("Mount", [typeof(Func<RenderContext, Element>)]);
        Assert.NotNull(method);
    }

    [Fact]
    public void Host_Constructor_Accepts_Logger()
    {
        var ctor = typeof(ReactorHost).GetConstructor(
            [typeof(Microsoft.UI.Xaml.Window), typeof(ILogger)]);
        Assert.NotNull(ctor);
    }

    // ── Reconciler used by hosts ─────────────────────────────────

    [Fact]
    public void Reconciler_Accepts_Logger()
    {
        var reconciler = new Reconciler(NullLogger.Instance);
        Assert.NotNull(reconciler);
    }

    // ── Render loop limit constant ───────────────────────────────

    [Fact]
    public void Host_Has_MaxRenderIterations_Field()
    {
        // Verify the render loop limit exists (it was added per feedback 7.5)
        var field = typeof(ReactorHost).GetField("MaxRenderIterations",
            global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Static);
        Assert.NotNull(field);
        Assert.Equal(50, field!.GetValue(null));
    }

    [Fact]
    public void HostControl_Has_MaxRenderIterations_Field()
    {
        var field = typeof(ReactorHostControl).GetField("MaxRenderIterations",
            global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Static);
        Assert.NotNull(field);
        Assert.Equal(50, field!.GetValue(null));
    }
}
