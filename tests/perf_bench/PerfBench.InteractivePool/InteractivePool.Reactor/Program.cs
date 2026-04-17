using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using PerfBench.Shared;

var opts = BenchCliOptions.Parse(args);
if (opts.Headless) ConsoleHelper.EnsureConsole();
InteractivePoolApp.Opts = opts;
ReactorApp.Run<InteractivePoolApp>("EXP-6 InteractivePool.Reactor", fullScreen: true, configure: host =>
{
    if (opts.Optimization == "off")
        host.Reconciler.Pool.Enabled = false;
});
