using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using PerfBench.Shared;

namespace PerfBench.InteractivePool.Bound;

public sealed class MainWindow : Window
{
    private const int ItemCount = 500;

    private readonly BenchCliOptions _opts;
    private readonly BenchTracker _tracker = new();
    private readonly InteractiveItemViewModel[] _vms = new InteractiveItemViewModel[ItemCount];
    private readonly ScrollViewer _scrollViewer;
    private double _scrollOffset;

    private readonly TextBlock _hudFps = new();
    private readonly TextBlock _hudUpdate = new();
    private readonly TextBlock _hudMemory = new();

    public MainWindow(BenchCliOptions opts)
    {
        _opts = opts;
        Title = "EXP-6 InteractivePool Bound";
        _tracker.ResetGcBaseline();

        var root = new StackPanel();

        // HUD
        var hud = new StackPanel { Orientation = Orientation.Horizontal, Padding = new Thickness(4) };
        _hudFps.Width = 120;
        _hudUpdate.Width = 150;
        _hudMemory.Width = 150;
        hud.Children.Add(_hudFps);
        hud.Children.Add(_hudUpdate);
        hud.Children.Add(_hudMemory);
        root.Children.Add(hud);

        // ScrollViewer + StackPanel with 500 interactive rows
        _scrollViewer = new ScrollViewer { Height = 800 };
        var panel = new StackPanel();

        _tracker.BeginMount();
        for (int i = 0; i < ItemCount; i++)
        {
            _vms[i] = new InteractiveItemViewModel
            {
                ButtonLabel = $"Action {i}",
                TextValue = $"Item {i}",
                IsToggled = i % 2 == 0
            };

            var row = new StackPanel { Orientation = Orientation.Horizontal, Padding = new Thickness(2), Spacing = 8 };

            var btn = new Button { MinWidth = 80 };
            btn.SetBinding(Button.ContentProperty, new Binding
            {
                Source = _vms[i],
                Path = new PropertyPath("ButtonLabel"),
                Mode = BindingMode.OneWay
            });
            row.Children.Add(btn);

            var tb = new TextBox { Width = 150 };
            tb.SetBinding(TextBox.TextProperty, new Binding
            {
                Source = _vms[i],
                Path = new PropertyPath("TextValue"),
                Mode = BindingMode.TwoWay
            });
            row.Children.Add(tb);

            var ts = new ToggleSwitch();
            ts.SetBinding(ToggleSwitch.IsOnProperty, new Binding
            {
                Source = _vms[i],
                Path = new PropertyPath("IsToggled"),
                Mode = BindingMode.TwoWay
            });
            row.Children.Add(ts);

            panel.Children.Add(row);
        }
        _tracker.EndMount();

        _scrollViewer.Content = panel;
        root.Children.Add(_scrollViewer);
        Content = root;

        CompositionTarget.Rendering += (_, _) =>
        {
            _tracker.FrameRendered();
            _hudFps.Text = $"FPS: {_tracker.CurrentFps:F0}";
            _hudUpdate.Text = $"Update: {_tracker.LastUpdateMs:F2}ms";
            _hudMemory.Text = $"Mem: {_tracker.CurrentMemoryMB}MB";
        };

        // Programmatic scroll at 16ms
        var scrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        scrollTimer.Tick += (_, _) =>
        {
            _scrollOffset += 20;
            _scrollViewer.ChangeView(null, _scrollOffset, null, true);
        };
        scrollTimer.Start();

        if (_opts.Headless)
        {
            var shutdown = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_opts.DurationSeconds) };
            shutdown.Tick += (_, _) =>
            {
                shutdown.Stop();
                scrollTimer.Stop();
                _tracker.WriteReportFile("EXP6_InteractivePool_Bound");
                Close();
            };
            shutdown.Start();
        }
    }
}
