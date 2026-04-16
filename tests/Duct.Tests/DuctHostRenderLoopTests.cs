using Duct.Core;
using Microsoft.UI.Xaml.Controls;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for DuctHost and DuctHostControl API surface and contracts.
/// Runtime lifecycle tests (mounting, rendering, state changes) require a XAML
/// Application context and UI thread — those are covered by the DuctTestApp integration tests.
/// </summary>
public class DuctHostRenderLoopTests
{
    // ── DuctHostControl API surface ──────────────────────────────

    [Fact]
    public void DuctHostControl_Constructor_Accepts_Logger()
    {
        var ctor = typeof(DuctHostControl).GetConstructor([typeof(Component), typeof(IDuctLogger)]);
        Assert.NotNull(ctor);
        // Both parameters should be optional
        foreach (var param in ctor!.GetParameters())
            Assert.True(param.HasDefaultValue, $"Parameter '{param.Name}' should have a default value");
    }

    [Fact]
    public void DuctHostControl_Has_Default_Constructor()
    {
        // Constructor has (Component?, IDuctLogger?) — both optional, so callable with no args
        var ctor = typeof(DuctHostControl).GetConstructor([typeof(Component), typeof(IDuctLogger)]);
        Assert.NotNull(ctor);
        Assert.True(ctor!.GetParameters().All(p => p.HasDefaultValue));
    }

    // ── DuctHost API surface ─────────────────────────────────────

    [Fact]
    public void DuctHost_Has_Mount_Component_Method()
    {
        var method = typeof(DuctHost).GetMethod("Mount", [typeof(Component)]);
        Assert.NotNull(method);
    }

    [Fact]
    public void DuctHost_Has_Mount_Func_Method()
    {
        var method = typeof(DuctHost).GetMethod("Mount", [typeof(Func<RenderContext, Element>)]);
        Assert.NotNull(method);
    }

    [Fact]
    public void DuctHost_Constructor_Accepts_Logger()
    {
        var ctor = typeof(DuctHost).GetConstructor(
            [typeof(Microsoft.UI.Xaml.Window), typeof(IDuctLogger)]);
        Assert.NotNull(ctor);
    }

    // ── Reconciler used by hosts ─────────────────────────────────

    [Fact]
    public void Reconciler_Accepts_Logger()
    {
        var logger = new NullDuctLogger();
        var reconciler = new Reconciler(logger);
        Assert.NotNull(reconciler);
    }

    // ── Render loop limit constant ───────────────────────────────

    [Fact]
    public void DuctHost_Has_MaxRenderIterations_Field()
    {
        // Verify the render loop limit exists (it was added per feedback 7.5)
        var field = typeof(DuctHost).GetField("MaxRenderIterations",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(field);
        Assert.Equal(50, field!.GetValue(null));
    }

    [Fact]
    public void DuctHostControl_Has_MaxRenderIterations_Field()
    {
        var field = typeof(DuctHostControl).GetField("MaxRenderIterations",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(field);
        Assert.Equal(50, field!.GetValue(null));
    }
}
