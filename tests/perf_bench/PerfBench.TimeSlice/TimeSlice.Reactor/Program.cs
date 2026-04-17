using Microsoft.UI.Reactor;
using PerfBench.TimeSlice.Reactor;
using PerfBench.Shared;

var opts = BenchCliOptions.Parse(args);
if (opts.Headless) ConsoleHelper.EnsureConsole();
TimeSliceApp.Opts = opts;
ReactorApp.Run<TimeSliceApp>("EXP-7 TimeSlice.Reactor", fullScreen: true);
