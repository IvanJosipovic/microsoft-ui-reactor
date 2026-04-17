using Microsoft.UI.Reactor;
using PerfBench.Shared;
using PerfBench.StructuralSharing.Reactor;

var opts = BenchCliOptions.Parse(args);
if (opts.Headless) ConsoleHelper.EnsureConsole();
StructuralSharingApp.Opts = opts;
ReactorApp.Run<StructuralSharingApp>("EXP-3 StructuralSharing.Reactor", fullScreen: true);
