using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Microsoft.UI.Reactor.Cli.Devtools;

/// <summary>
/// Thin JSON-RPC client for the devtools MCP endpoint. One
/// <see cref="InvokeTool"/> entry point; everything else in the CLI
/// (named verbs, the generic <c>call</c> escape hatch) layers on top.
/// Spec 025 §4.
/// </summary>
internal sealed class McpCliClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _endpoint;

    public McpCliClient(string endpoint, TimeSpan? timeout = null)
    {
        _endpoint = endpoint;
        _http = new HttpClient { Timeout = timeout ?? TimeSpan.FromSeconds(30) };
    }

    public void Dispose() => _http.Dispose();

    public JsonDocument InvokeTool(string toolName, JsonElement? arguments)
    {
        var payload = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new
            {
                name = toolName,
                arguments = arguments ?? JsonDocument.Parse("{}").RootElement,
            },
        };
        return Post(payload);
    }

    public JsonDocument InvokeMethod(string method, JsonElement? @params)
    {
        var payload = new
        {
            jsonrpc = "2.0",
            id = 1,
            method,
            @params = @params,
        };
        return Post(payload);
    }

    private JsonDocument Post(object payload)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = global::System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var resp = _http.PostAsync(_endpoint, content).GetAwaiter().GetResult();
        var body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return JsonDocument.Parse(body);
    }
}
