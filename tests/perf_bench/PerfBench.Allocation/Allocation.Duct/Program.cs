using Duct;
using PerfBench.Allocation.Duct;
using PerfBench.Shared;

var opts = BenchCliOptions.Parse(args);
if (opts.Headless) ConsoleHelper.EnsureConsole();
AllocationApp.Opts = opts;
DuctApp.Run<AllocationApp>("EXP-10 Allocation.Duct", fullScreen: true);
