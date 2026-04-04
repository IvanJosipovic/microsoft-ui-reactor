using Duct;
using Duct.Core;
using PerfBench.Shared;

var opts = BenchCliOptions.Parse(args);
if (opts.Headless) ConsoleHelper.EnsureConsole();
InteractivePoolApp.Opts = opts;
DuctApp.Run<InteractivePoolApp>("EXP6 InteractivePool Duct", fullScreen: true, configure: host =>
{
    if (opts.Optimization == "off")
        host.Reconciler.Pool.Enabled = false;
});
