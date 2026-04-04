using Duct;
using Duct.Core;
using PerfBench.Shared;

var opts = BenchCliOptions.Parse(args);
if (opts.Headless) ConsoleHelper.EnsureConsole();
JournalApp.Opts = opts;
DuctApp.Run<JournalApp>("EXP5 Journal Duct", fullScreen: true);
