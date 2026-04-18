using Microsoft.UI.Reactor.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.AnalyzerTests;

/// <summary>
/// Unit tests for the REACTOR_HOOKS_001/004/005 analyzer. Tests embed minimal stub
/// types in the <c>Microsoft.UI.Reactor.Core</c> namespace so the analyzer's
/// fully-qualified-name receiver check fires without pulling in the real framework.
/// </summary>
public class HookRulesAnalyzerTests
{
    private const string Stubs = @"
namespace Microsoft.UI.Reactor.Core
{
    public class RenderContext { }

    public abstract class Component
    {
        protected internal RenderContext Context { get; } = new RenderContext();
        public abstract string Render();
        protected (int, System.Action<int>) UseState(int initial) => (0, _ => { });
        protected void UseEffect(System.Action effect, params object[] deps) { }
        protected T UseMemo<T>(System.Func<T> factory, params object[] deps) => factory();
    }
}

namespace Microsoft.UI.Reactor.Hooks
{
    using Microsoft.UI.Reactor.Core;
    public static class Extensions
    {
        public static int UseCustom(this RenderContext ctx, object[] deps) => 0;
    }
}
";

    private static DiagnosticResult Diagnostic(string id) =>
        CSharpAnalyzerVerifier<HookRulesAnalyzer, DefaultVerifier>.Diagnostic(id);

    // ── REACTOR_HOOKS_001 — conditional hook ──────────────────────────

    [Fact]
    public async Task Hook_Inside_If_Flags_Conditional()
    {
        var test = Stubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    public override string Render()
    {
        if (System.DateTime.Now.Ticks > 0)
        {
            var (count, setCount) = UseState(0);
        }
        return """";
    }
}";
        var expected = Diagnostic(HookRulesAnalyzer.ConditionalHookId)
            .WithSpan(31, 37, 31, 48)
            .WithArguments("UseState", "if");

        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
            ExpectedDiagnostics = { expected },
        };
        await analyzerTest.RunAsync();
    }

    [Fact]
    public async Task Hook_Inside_ForEach_Flags_Conditional()
    {
        var test = Stubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    public override string Render()
    {
        foreach (var i in new[] { 1, 2, 3 })
        {
            var (count, setCount) = UseState(0);
        }
        return """";
    }
}";
        var expected = Diagnostic(HookRulesAnalyzer.ConditionalHookId)
            .WithSpan(31, 37, 31, 48)
            .WithArguments("UseState", "foreach loop");

        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
            ExpectedDiagnostics = { expected },
        };
        await analyzerTest.RunAsync();
    }

    [Fact]
    public async Task Hook_Inside_Lambda_Flags_Conditional()
    {
        var test = Stubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    public override string Render()
    {
        System.Action a = () => UseEffect(() => { }, new object[0]);
        return """";
    }
}";
        // The lambda UseEffect call triggers both the conditional rule (it's inside a
        // nested lambda) and the unstable-deps rule (the `new object[0]` deps literal).
        var conditional = Diagnostic(HookRulesAnalyzer.ConditionalHookId)
            .WithSpan(29, 33, 29, 68)
            .WithArguments("UseEffect", "nested lambda/local function");
        var unstableDeps = Diagnostic(HookRulesAnalyzer.UnstableDepsId)
            .WithSpan(29, 54, 29, 67)
            .WithArguments("UseEffect", "array");

        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
            ExpectedDiagnostics = { conditional, unstableDeps },
        };
        await analyzerTest.RunAsync();
    }

    [Fact]
    public async Task Hook_At_Top_Of_Render_No_Diagnostic()
    {
        var test = Stubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    public override string Render()
    {
        var (count, setCount) = UseState(0);
        return """";
    }
}";
        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
        };
        await analyzerTest.RunAsync();
    }

    // ── REACTOR_HOOKS_004 — freshly-allocated deps ────────────────────

    [Fact]
    public async Task UseEffect_With_New_List_Dep_Flags()
    {
        var test = Stubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    public override string Render()
    {
        UseEffect(() => { }, new System.Collections.Generic.List<int>());
        return """";
    }
}";
        var expected = Diagnostic(HookRulesAnalyzer.UnstableDepsId)
            .WithSpan(29, 30, 29, 72)
            .WithArguments("UseEffect", "object");

        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
            ExpectedDiagnostics = { expected },
        };
        await analyzerTest.RunAsync();
    }

    [Fact]
    public async Task UseEffect_With_Lambda_Dep_Flags()
    {
        var test = Stubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    public override string Render()
    {
        UseEffect(() => { }, (System.Func<int>)(() => 1));
        return """";
    }
}";
        var expected = Diagnostic(HookRulesAnalyzer.UnstableDepsId)
            .WithSpan(29, 30, 29, 57)
            .WithArguments("UseEffect", "lambda");

        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
            ExpectedDiagnostics = { expected },
        };
        await analyzerTest.RunAsync();
    }

    [Fact]
    public async Task UseEffect_With_Scalar_Dep_No_Diagnostic()
    {
        var test = Stubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    public override string Render()
    {
        int userId = 42;
        UseEffect(() => { }, userId);
        return """";
    }
}";
        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
        };
        await analyzerTest.RunAsync();
    }

    // ── REACTOR_HOOKS_005 — hook outside Render/custom hook ────────────

    [Fact]
    public async Task Hook_In_Event_Handler_Flags_Outside_Render()
    {
        var test = Stubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    void HandleClick()
    {
        var (count, setCount) = UseState(0);
    }
    public override string Render() => """";
}";
        var expected = Diagnostic(HookRulesAnalyzer.HookOutsideRenderId)
            .WithSpan(29, 33, 29, 44)
            .WithArguments("UseState");

        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
            ExpectedDiagnostics = { expected },
        };
        await analyzerTest.RunAsync();
    }

    [Fact]
    public async Task Hook_In_Custom_Use_Method_No_Diagnostic()
    {
        var test = Stubs + @"
class C : Microsoft.UI.Reactor.Core.Component
{
    int UseCustom()
    {
        var (count, setCount) = UseState(0);
        return count;
    }
    public override string Render()
    {
        var c = UseCustom();
        return """";
    }
}";
        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
        };
        await analyzerTest.RunAsync();
    }

    // ── Non-hook lookalike ────────────────────────────────────────────

    [Fact]
    public async Task Non_Reactor_Use_Method_Not_Flagged()
    {
        // `UseAuthentication`-style helpers outside Reactor's type hierarchy should be
        // left alone.
        var test = @"
class Builder
{
    public Builder UseAuthentication() => this;
}
class Program
{
    static void Main()
    {
        // Called outside Render, inside an if — but receiver isn't a Reactor type.
        var b = new Builder();
        if (true) { b.UseAuthentication(); }
    }
}";
        var analyzerTest = new CSharpAnalyzerTest<HookRulesAnalyzer, DefaultVerifier>
        {
            TestCode = test,
        };
        await analyzerTest.RunAsync();
    }
}
