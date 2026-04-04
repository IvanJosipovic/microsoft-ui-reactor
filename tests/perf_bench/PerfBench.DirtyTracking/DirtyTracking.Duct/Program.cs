using Duct;
using PerfBench.DirtyTracking.Duct;
using PerfBench.Shared;

var opts = BenchCliOptions.Parse(args);
if (opts.Headless) ConsoleHelper.EnsureConsole();
DirtyTrackingApp.Opts = opts;
DuctApp.Run<DirtyTrackingApp>("EXP-1 DirtyTracking.Duct", fullScreen: true);
