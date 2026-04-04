using Duct;
using PerfBench.Priorities.Duct;
using PerfBench.Shared;

var opts = BenchCliOptions.Parse(args);
if (opts.Headless) ConsoleHelper.EnsureConsole();
PrioritiesApp.Opts = opts;
DuctApp.Run<PrioritiesApp>("EXP-8 Priorities.Duct", fullScreen: true);
