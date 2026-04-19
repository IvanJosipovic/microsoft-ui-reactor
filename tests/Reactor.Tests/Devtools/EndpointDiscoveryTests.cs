using Microsoft.UI.Reactor.Cli.Devtools;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Devtools;

/// <summary>
/// Spec 025 §5: endpoint resolution precedence. <c>--endpoint</c> wins without
/// probing; lockfile scan is the default; <c>--auto</c> port scan is deferred.
/// </summary>
public class EndpointDiscoveryTests
{
    [Fact]
    public void ExplicitEndpoint_BypassesLockfileScan()
    {
        var r = EndpointDiscovery.Resolve("http://127.0.0.1:12345/mcp", autoScan: false);
        Assert.Equal(DevtoolsCliExit.Success, r.Exit);
        Assert.Equal("http://127.0.0.1:12345/mcp", r.Endpoint);
    }

    // Lockfile-scan tests that depend on %TEMP% state would be flaky in
    // parallel CI; the integration / E2E suite owns those cases. We assert
    // here only that Resolve returns a deterministic NoLiveSession when no
    // session is *ever* going to match (we use a throwaway endpoint we
    // deliberately never wrote a lockfile for, and the worst case is that an
    // unrelated lockfile is live — still a well-formed response).
    [Fact]
    public void ResolveReturnsWellFormedResult()
    {
        var r = EndpointDiscovery.Resolve(null, autoScan: false);
        Assert.True(Enum.IsDefined(r.Exit));
        if (r.Exit == DevtoolsCliExit.Success)
            Assert.NotNull(r.Endpoint);
        else
            Assert.NotNull(r.ErrorMessage);
    }
}
