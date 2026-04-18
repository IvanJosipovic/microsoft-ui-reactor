using System.Text.Json;
using Microsoft.UI.Reactor.Hosting.Devtools;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Devtools;

/// <summary>
/// Stdio MCP transport tests. The loop is transport-shaped around an
/// in-memory TextReader/TextWriter so we can exercise framing, EOF, and
/// request/response parity without spawning a child process. The deeper
/// parity pass (running the Phase 2+3 self-host suite over stdio) lands
/// with §4.7.
/// </summary>
public class StdioTransportTests
{
    private static McpDispatcher BuildDispatcher()
    {
        var reg = new McpToolRegistry();
        reg.Register(
            new McpToolDescriptor("ping", "", new { type = "object" }),
            _ => new { ok = true });
        reg.Register(
            new McpToolDescriptor("echo", "", new { type = "object" }),
            p => new { echoed = DevtoolsTools.ReadString(p, "text") });
        return new McpDispatcher(reg);
    }

    [Fact]
    public void ProcessLine_ValidRequest_EmitsSingleLineJsonResponse()
    {
        var loop = new StdioMcpLoop(BuildDispatcher(), new StringReader(""), new StringWriter());
        var response = loop.ProcessLine(
            """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"ping"}}""");

        // One-line JSON framing: no embedded newlines.
        Assert.DoesNotContain('\n', response);
        using var doc = JsonDocument.Parse(response);
        Assert.Equal(1, doc.RootElement.GetProperty("id").GetInt32());
        Assert.True(doc.RootElement.GetProperty("result").GetProperty("ok").GetBoolean());
    }

    [Fact]
    public void ProcessLine_MalformedRequest_ReturnsParseError()
    {
        var loop = new StdioMcpLoop(BuildDispatcher(), new StringReader(""), new StringWriter());
        var response = loop.ProcessLine("not json");
        using var doc = JsonDocument.Parse(response);
        var error = doc.RootElement.GetProperty("error");
        Assert.Equal(JsonRpcErrorCodes.ParseError, error.GetProperty("code").GetInt32());
    }

    [Fact]
    public void Run_ProcessesAllLinesUntilEof()
    {
        // Three requests, each on its own line; the loop should emit three
        // response lines, preserving id order.
        var input =
            """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"ping"}}""" + "\n" +
            """{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"echo","arguments":{"text":"hi"}}}""" + "\n" +
            """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"ping"}}""" + "\n";

        var writer = new StringWriter();
        var loop = new StdioMcpLoop(BuildDispatcher(), new StringReader(input), writer);
        loop.Run(CancellationToken.None);

        var lines = writer.ToString()
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        for (int i = 0; i < lines.Length; i++)
        {
            using var doc = JsonDocument.Parse(lines[i]);
            Assert.Equal(i + 1, doc.RootElement.GetProperty("id").GetInt32());
        }
    }

    [Fact]
    public void Run_BlankLinesAreSkipped()
    {
        var input = "\n\n" +
            """{"jsonrpc":"2.0","id":7,"method":"tools/call","params":{"name":"ping"}}""" + "\n\n";
        var writer = new StringWriter();
        var loop = new StdioMcpLoop(BuildDispatcher(), new StringReader(input), writer);
        loop.Run(CancellationToken.None);

        var lines = writer.ToString()
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
        using var doc = JsonDocument.Parse(lines[0]);
        Assert.Equal(7, doc.RootElement.GetProperty("id").GetInt32());
    }

    [Fact]
    public void Run_ExitsCleanlyWhenCancellationFiresBeforeRead()
    {
        // A pre-cancelled token terminates the loop without reading a line.
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var writer = new StringWriter();
        var loop = new StdioMcpLoop(
            BuildDispatcher(),
            new StringReader("""{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"ping"}}""" + "\n"),
            writer);
        loop.Run(cts.Token);
        Assert.Empty(writer.ToString().Trim());
    }

    [Fact]
    public void ResponseShape_MatchesHttpTransport()
    {
        // Parity: an identical dispatcher fed the same request line through
        // the stdio loop and the HTTP-path dispatcher must produce identical
        // response JSON. This keeps the transports honest.
        var request = """{"jsonrpc":"2.0","id":99,"method":"tools/call","params":{"name":"echo","arguments":{"text":"same"}}}""";

        var stdioLoop = new StdioMcpLoop(BuildDispatcher(), new StringReader(""), new StringWriter());
        var viaStdio = stdioLoop.ProcessLine(request);
        var viaHttp = JsonSerializer.Serialize(BuildDispatcher().Dispatch(request), DevtoolsMcpServer.JsonOpts);

        Assert.Equal(viaHttp, viaStdio);
    }
}
