using Microsoft.UI.Reactor.Cli.Devtools;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Devtools;

// Pure arg-parsing tests for the supervisor — the `dotnet run`/`dotnet build`
// spawn paths are exercised by the Phase 2.18 E2E Appium suite.
public class SupervisorArgsTests
{
    [Fact]
    public void Empty_ParsesToAllNulls()
    {
        var r = DevtoolsSupervisor.ParseArgs([]);
        Assert.Null(r.Project);
        Assert.Null(r.Component);
        Assert.Null(r.McpPort);
        Assert.False(r.Help);
        Assert.Null(r.Error);
    }

    [Fact]
    public void PositionalProject()
    {
        var r = DevtoolsSupervisor.ParseArgs(["./My.csproj"]);
        Assert.Equal("./My.csproj", r.Project);
    }

    [Fact]
    public void ComponentFlag()
    {
        var r = DevtoolsSupervisor.ParseArgs(["--component", "CounterDemo"]);
        Assert.Equal("CounterDemo", r.Component);
    }

    [Fact]
    public void McpPortFlag()
    {
        var r = DevtoolsSupervisor.ParseArgs(["--mcp-port", "54321"]);
        Assert.Equal(54321, r.McpPort);
    }

    [Fact]
    public void McpPortFlag_NonNumeric_SetsError()
    {
        var r = DevtoolsSupervisor.ParseArgs(["--mcp-port", "nope"]);
        Assert.NotNull(r.Error);
        Assert.Contains("--mcp-port", r.Error);
    }

    [Fact]
    public void HelpFlag()
    {
        var r = DevtoolsSupervisor.ParseArgs(["--help"]);
        Assert.True(r.Help);
    }

    [Fact]
    public void UnknownFlag_SetsError()
    {
        var r = DevtoolsSupervisor.ParseArgs(["--wat"]);
        Assert.NotNull(r.Error);
    }

    [Fact]
    public void TwoPositionals_SetsError()
    {
        var r = DevtoolsSupervisor.ParseArgs(["a.csproj", "b.csproj"]);
        Assert.NotNull(r.Error);
    }

    [Fact]
    public void AllCombined_Parses()
    {
        var r = DevtoolsSupervisor.ParseArgs(["./x.csproj", "--component", "C", "--mcp-port", "9000"]);
        Assert.Equal("./x.csproj", r.Project);
        Assert.Equal("C", r.Component);
        Assert.Equal(9000, r.McpPort);
    }

    [Fact]
    public void PrintConfigFlag_Parses()
    {
        var r = DevtoolsSupervisor.ParseArgs(["--print-config", "--mcp-port", "12345"]);
        Assert.True(r.PrintConfig);
        Assert.Equal(12345, r.McpPort);
        Assert.Null(r.Error);
    }

    [Fact]
    public void PrintConfigFlag_AloneIsAllowed()
    {
        // Without --mcp-port the supervisor will pick one at Run() time.
        var r = DevtoolsSupervisor.ParseArgs(["--print-config"]);
        Assert.True(r.PrintConfig);
        Assert.Null(r.McpPort);
    }
}
