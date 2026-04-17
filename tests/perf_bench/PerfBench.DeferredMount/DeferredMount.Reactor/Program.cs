using Microsoft.UI.Reactor;
using PerfBench.DeferredMount.Reactor;
using PerfBench.Shared;

var opts = BenchCliOptions.Parse(args);
if (opts.Headless) ConsoleHelper.EnsureConsole();
DeferredMountApp.Opts = opts;
ReactorApp.Run<DeferredMountApp>("EXP-9 DeferredMount.Reactor", fullScreen: true);
