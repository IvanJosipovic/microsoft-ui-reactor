using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.UI.Reactor.Cli.Devtools;

/// <summary>
/// <c>mur devtools [project] [--component Name] [--mcp-port N]</c>.
/// Launches <c>dotnet run -- --devtools run</c> against the target project and
/// respawns after each exit-code-42 from the child (the reload sentinel).
/// </summary>
internal sealed record SupervisorArgs(string? Project, string? Component, int? McpPort, bool Help, string? Error);

internal static class DevtoolsSupervisor
{
    private const int ReloadExitCode = 42;

    internal static SupervisorArgs ParseArgs(string[] args)
    {
        string? project = null;
        string? component = null;
        int? mcpPort = null;

        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "--component" && i + 1 < args.Length)
            {
                component = args[++i];
            }
            else if (a == "--mcp-port" && i + 1 < args.Length)
            {
                if (!int.TryParse(args[++i], out var p))
                    return new SupervisorArgs(null, null, null, false, "Invalid --mcp-port value.");
                mcpPort = p;
            }
            else if (a == "--help" || a == "-h")
            {
                return new SupervisorArgs(null, null, null, true, null);
            }
            else if (a.StartsWith("-"))
            {
                return new SupervisorArgs(null, null, null, false, $"Unknown flag: {a}");
            }
            else if (project is null)
            {
                project = a;
            }
            else
            {
                return new SupervisorArgs(null, null, null, false, $"Unexpected argument: {a}");
            }
        }

        return new SupervisorArgs(project, component, mcpPort, false, null);
    }

    public static int Run(string[] args)
    {
        var parsed = ParseArgs(args);
        if (parsed.Help)
        {
            PrintHelp();
            return 0;
        }
        if (parsed.Error is not null)
        {
            Console.Error.WriteLine($"[mur devtools] {parsed.Error}");
            return 1;
        }

        var project = parsed.Project ?? FindDefaultProject(Directory.GetCurrentDirectory());
        var component = parsed.Component;
        var mcpPort = parsed.McpPort;
        if (project is null)
        {
            Console.Error.WriteLine("[mur devtools] No .csproj found in the current directory.");
            return 1;
        }

        // Pin an MCP port across respawns so the agent keeps a stable endpoint.
        var pinnedPort = mcpPort ?? FindFreePort();
        Console.WriteLine($"[mur devtools] Using MCP port {pinnedPort} across reloads.");

        while (true)
        {
            var exitCode = LaunchChild(project, component, pinnedPort);
            if (exitCode == ReloadExitCode)
            {
                Console.WriteLine("[mur devtools] Reload requested — rebuilding...");
                var buildOk = RunDotnetBuild(project);
                if (!buildOk)
                {
                    Console.Error.WriteLine("[mur devtools] Build failed. Waiting — fix the error and request reload again.");
                    // Deliberately do NOT respawn: the spec says the MCP port
                    // stays unbound on build failure so the agent sees a transport
                    // error. We exit the supervisor with the build-fail code; the
                    // user re-runs `mur devtools` when they're ready.
                    return 2;
                }
                continue;
            }
            return exitCode;
        }
    }

    private static int LaunchChild(string project, string? component, int mcpPort)
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = false, // Stream verbatim; don't buffer.
            RedirectStandardError = false,
            WorkingDirectory = Directory.GetCurrentDirectory(),
        };
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add(project);
        psi.ArgumentList.Add("--");
        psi.ArgumentList.Add("--devtools");
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--mcp-port");
        psi.ArgumentList.Add(mcpPort.ToString());
        if (!string.IsNullOrEmpty(component))
            psi.ArgumentList.Add(component);

        Console.WriteLine($"[mur devtools] Launching: dotnet {string.Join(' ', psi.ArgumentList)}");
        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet.");
        proc.WaitForExit();
        return proc.ExitCode;
    }

    private static bool RunDotnetBuild(string project)
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            WorkingDirectory = Directory.GetCurrentDirectory(),
        };
        psi.ArgumentList.Add("build");
        psi.ArgumentList.Add(project);

        using var proc = Process.Start(psi);
        if (proc is null) return false;
        proc.WaitForExit();
        return proc.ExitCode == 0;
    }

    private static string? FindDefaultProject(string dir)
    {
        try
        {
            var hits = Directory.GetFiles(dir, "*.csproj", SearchOption.TopDirectoryOnly);
            return hits.Length == 1 ? hits[0] : null;
        }
        catch
        {
            return null;
        }
    }

    private static int FindFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("mur devtools [project] [--component Name] [--mcp-port N]");
        Console.WriteLine();
        Console.WriteLine("  Launches the target project with --devtools run and respawns on reload.");
        Console.WriteLine("  When the child exits with code 42, rebuilds and relaunches. Any other");
        Console.WriteLine("  exit code propagates. The MCP port is pinned across respawns so an");
        Console.WriteLine("  agent can reconnect at the same endpoint.");
    }
}
