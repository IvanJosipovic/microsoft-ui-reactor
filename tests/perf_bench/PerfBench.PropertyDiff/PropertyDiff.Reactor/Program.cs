using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using PerfBench.PropertyDiff.Reactor;
using PerfBench.Shared;

var opts = BenchCliOptions.Parse(args);
if (opts.Headless) ConsoleHelper.EnsureConsole();
PropertyDiffApp.Opts = opts;

// EXP-2: toggle bitmask diff optimization via --optimization on|off
Reconciler.EnableBitmaskDiff = opts.Optimization == "on";
Console.WriteLine($"EXP-2 BitmaskDiff: {(Reconciler.EnableBitmaskDiff ? "ON" : "OFF")}");

ReactorApp.Run<PropertyDiffApp>("EXP-2 PropertyDiff.Reactor", fullScreen: true);
