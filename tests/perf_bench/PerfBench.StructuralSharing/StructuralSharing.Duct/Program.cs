using Duct;
using PerfBench.Shared;
using PerfBench.StructuralSharing.Duct;

var opts = BenchCliOptions.Parse(args);
if (opts.Headless) ConsoleHelper.EnsureConsole();
StructuralSharingApp.Opts = opts;
DuctApp.Run<StructuralSharingApp>("EXP-3 StructuralSharing.Duct", fullScreen: true);
