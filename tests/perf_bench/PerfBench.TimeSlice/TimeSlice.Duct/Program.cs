using Duct;
using PerfBench.TimeSlice.Duct;
using PerfBench.Shared;

var opts = BenchCliOptions.Parse(args);
if (opts.Headless) ConsoleHelper.EnsureConsole();
TimeSliceApp.Opts = opts;
DuctApp.Run<TimeSliceApp>("EXP-7 TimeSlice.Duct", fullScreen: true);
