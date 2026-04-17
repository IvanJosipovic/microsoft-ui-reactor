using Microsoft.UI.Reactor;
using PerfBench.Priorities.Reactor;
using PerfBench.Shared;

var opts = BenchCliOptions.Parse(args);
if (opts.Headless) ConsoleHelper.EnsureConsole();
PrioritiesApp.Opts = opts;
ReactorApp.Run<PrioritiesApp>("EXP-8 Priorities.Reactor", fullScreen: true);
