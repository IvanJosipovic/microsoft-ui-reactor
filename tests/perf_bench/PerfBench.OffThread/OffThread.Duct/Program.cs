using Duct;
using Duct.Core;
using PerfBench.Shared;

var opts = BenchCliOptions.Parse(args);
if (opts.Headless) ConsoleHelper.EnsureConsole();
OffThreadApp.Opts = opts;
DuctApp.Run<OffThreadApp>("EXP4 OffThread Duct", fullScreen: true);
