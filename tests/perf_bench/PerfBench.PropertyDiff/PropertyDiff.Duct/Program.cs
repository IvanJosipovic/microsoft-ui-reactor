using Duct;
using PerfBench.PropertyDiff.Duct;
using PerfBench.Shared;

var opts = BenchCliOptions.Parse(args);
if (opts.Headless) ConsoleHelper.EnsureConsole();
PropertyDiffApp.Opts = opts;
DuctApp.Run<PropertyDiffApp>("EXP-2 PropertyDiff.Duct", fullScreen: true);
