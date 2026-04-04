using Microsoft.UI.Xaml;
using PerfBench.Shared;

namespace PerfBench.DeferredMount.Bound;

public partial class App : Application
{
    public static BenchCliOptions Options { get; set; } = new();
    public App() => InitializeComponent();
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = new MainWindow(Options);
        window.Activate();
    }
    [STAThread]
    public static void Main(string[] args)
    {
        Options = BenchCliOptions.Parse(args);
        if (Options.Headless) ConsoleHelper.EnsureConsole();
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
