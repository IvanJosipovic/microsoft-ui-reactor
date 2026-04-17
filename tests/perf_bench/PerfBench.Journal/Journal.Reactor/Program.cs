using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using PerfBench.Shared;

var opts = BenchCliOptions.Parse(args);
if (opts.Headless) ConsoleHelper.EnsureConsole();
JournalApp.Opts = opts;
ReactorApp.Run<JournalApp>("EXP-5 Journal.Reactor", fullScreen: true);
