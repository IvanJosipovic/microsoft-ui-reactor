using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using PerfBench.Shared;

var opts = BenchCliOptions.Parse(args);
if (opts.Headless) ConsoleHelper.EnsureConsole();
OffThreadApp.Opts = opts;
ReactorApp.Run<OffThreadApp>("EXP-4 OffThread.Reactor", fullScreen: true);
