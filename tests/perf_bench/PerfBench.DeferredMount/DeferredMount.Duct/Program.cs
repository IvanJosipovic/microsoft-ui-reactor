using Duct;
using PerfBench.DeferredMount.Duct;
using PerfBench.Shared;

var opts = BenchCliOptions.Parse(args);
if (opts.Headless) ConsoleHelper.EnsureConsole();
DeferredMountApp.Opts = opts;
DuctApp.Run<DeferredMountApp>("EXP-9 DeferredMount.Duct", fullScreen: true);
