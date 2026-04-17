using Microsoft.UI.Reactor;
using PerfBench.Allocation.Reactor;
using PerfBench.Shared;

var opts = BenchCliOptions.Parse(args);
if (opts.Headless) ConsoleHelper.EnsureConsole();
AllocationApp.Opts = opts;
ReactorApp.Run<AllocationApp>("EXP-10 Allocation.Reactor", fullScreen: true);
