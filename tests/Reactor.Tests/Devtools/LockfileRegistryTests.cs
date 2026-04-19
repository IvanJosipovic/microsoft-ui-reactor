using System.Text.Json;
using Microsoft.UI.Reactor.Hosting.Devtools;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Devtools;

/// <summary>
/// Pure tests for the spec 025 lockfile contract. Avoid touching %TEMP% by
/// writing to xunit-scoped temp directories; the tested surface takes a path.
/// </summary>
public class LockfileRegistryTests
{
    [Fact]
    public void Canonicalize_NormalizesDriveCaseAndSeparators()
    {
        var a = LockfileRegistry.Canonicalize(@"C:\Foo\Bar.csproj");
        var b = LockfileRegistry.Canonicalize(@"c:/Foo/Bar.csproj");
        Assert.Equal(a, b);
    }

    [Fact]
    public void ComputeHash_IsStableAndTruncatedTo16Hex()
    {
        var hash = LockfileRegistry.ComputeHash(@"C:\MyApp\MyApp.csproj");
        Assert.Equal(16, hash.Length);
        Assert.Matches("^[0-9a-f]{16}$", hash);
        Assert.Equal(hash, LockfileRegistry.ComputeHash(@"C:\MyApp\MyApp.csproj"));
    }

    [Fact]
    public void ComputeHash_DifferentPaths_DifferentHashes()
    {
        var a = LockfileRegistry.ComputeHash(@"C:\App1\App1.csproj");
        var b = LockfileRegistry.ComputeHash(@"C:\App2\App2.csproj");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Write_And_TryRead_RoundTrip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"reactor-test-{Guid.NewGuid():N}.json");
        try
        {
            var entry = new LockfileEntry
            {
                Endpoint = "http://127.0.0.1:54931/mcp",
                Transport = "http",
                Port = 54931,
                Pid = 12345,
                BuildTag = "2026-04-19T00:00:00Z",
                Project = @"C:\Users\me\MyApp.csproj",
                StartedAt = "2026-04-19T00:00:01Z",
            };
            LockfileRegistry.Write(path, entry);
            Assert.True(LockfileRegistry.TryRead(path, out var read));
            Assert.NotNull(read);
            Assert.Equal(entry.Endpoint, read!.Endpoint);
            Assert.Equal(entry.Port, read.Port);
            Assert.Equal(entry.Pid, read.Pid);
            Assert.Equal(entry.Project, read.Project);
            Assert.Equal(LockfileRegistry.SchemaTag, read.Schema);
        }
        finally { try { File.Delete(path); } catch { } }
    }

    [Fact]
    public void IsLive_DeadPid_ReturnsFalse()
    {
        // Pid 0 is never a live process in user-mode; some systems treat it
        // as the kernel. Either way Process.GetProcessById throws.
        var entry = new LockfileEntry { Pid = 0, Endpoint = "http://127.0.0.1:1/mcp", Transport = "http" };
        Assert.False(LockfileRegistry.IsLive(entry));
    }

    [Fact]
    public void IsLive_AlivePidButBadEndpoint_ReturnsFalse()
    {
        // Our own pid exists, but nothing is listening on the endpoint — the
        // HTTP probe has to fail even if the pid is alive.
        var entry = new LockfileEntry
        {
            Pid = global::System.Diagnostics.Process.GetCurrentProcess().Id,
            Endpoint = "http://127.0.0.1:1/mcp",
            Transport = "http",
        };
        Assert.False(LockfileRegistry.IsLive(entry));
    }

    [Fact]
    public void TryRead_MissingFile_ReturnsFalse()
    {
        var path = Path.Combine(Path.GetTempPath(), $"no-such-{Guid.NewGuid():N}.json");
        Assert.False(LockfileRegistry.TryRead(path, out var e));
        Assert.Null(e);
    }

    [Fact]
    public void TryDelete_MissingFile_DoesNotThrow()
    {
        var path = Path.Combine(Path.GetTempPath(), $"no-such-{Guid.NewGuid():N}.json");
        var ex = Record.Exception(() => LockfileRegistry.TryDelete(path));
        Assert.Null(ex);
    }
}
