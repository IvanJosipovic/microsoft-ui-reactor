using Microsoft.UI.Reactor.Cli.Devtools;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Devtools;

/// <summary>
/// Pure tests for spec 025 CLI verb argv handling. Covers the shared-flag
/// splitter and the known-verbs registry (the dispatch guard the top-level
/// <c>mur devtools</c> router uses). Actual HTTP calls belong in E2E.
/// </summary>
public class DevtoolsVerbsTests
{
    [Fact]
    public void ExtractShared_PicksOutEndpointAndFlags()
    {
        var (shared, rest) = DevtoolsVerbs.ExtractShared([
            "--endpoint", "http://127.0.0.1:1/mcp", "--pretty", "#btn-inc"
        ]);
        Assert.Equal("http://127.0.0.1:1/mcp", shared.Endpoint);
        Assert.True(shared.Pretty);
        Assert.False(shared.AutoScan);
        Assert.Equal(new[] { "#btn-inc" }, rest);
    }

    [Fact]
    public void ExtractShared_NoSharedFlags_PassesThroughVerbArgs()
    {
        var (shared, rest) = DevtoolsVerbs.ExtractShared(["#btn-inc", "hello"]);
        Assert.Null(shared.Endpoint);
        Assert.False(shared.Pretty);
        Assert.Equal(new[] { "#btn-inc", "hello" }, rest);
    }

    [Fact]
    public void KnownVerbs_ContainsEveryMcpToolFromSpec()
    {
        // §8 — one verb per MCP tool. Rename aliases: components (was `list`),
        // switch (was switchComponent), wait (was waitFor).
        foreach (var v in new[]
        {
            "version", "windows", "components", "switch", "tree", "screenshot",
            "state", "click", "type", "focus", "invoke", "toggle", "select",
            "scroll", "expand", "collapse", "wait", "fire", "reload", "shutdown",
            "call",
        })
            Assert.Contains(v, DevtoolsVerbs.KnownVerbs);
    }
}
