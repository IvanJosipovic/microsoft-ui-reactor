using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using PerfBench.Shared;

namespace PerfBench.OffThread.Bound;

public sealed class MainWindow : Window
{
    private const int Columns = 40;
    private const int Rows = 25;
    private const int Count = Columns * Rows; // 1000

    private readonly BenchCliOptions _opts;
    private readonly BenchTracker _tracker = new();
    private readonly ItemViewModel[] _vms = new ItemViewModel[Count];
    private readonly Random _rng = new(42);

    private readonly TextBlock _hudFps = new();
    private readonly TextBlock _hudUpdate = new();
    private readonly TextBlock _hudMemory = new();
    private int _tick;

    public MainWindow(BenchCliOptions opts)
    {
        _opts = opts;
        Title = "EXP-4 OffThread Bound";
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

        // Grid
        var grid = new Grid();
        for (int c = 0; c < Columns; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int r = 0; r < Rows; r++)
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        for (int i = 0; i < Count; i++)
        {
            _vms[i] = new ItemViewModel();
            var tb = new TextBlock { FontSize = 10, Padding = new Thickness(1) };
            tb.SetBinding(TextBlock.TextProperty, new Binding
            {
                Source = _vms[i],
                Path = new PropertyPath("DisplayText"),
                Mode = BindingMode.OneWay
            });
            Grid.SetColumn(tb, i % Columns);
            Grid.SetRow(tb, i / Columns);
            grid.Children.Add(tb);
        }
        root.Children.Add(grid);
        Content = root;

        CompositionTarget.Rendering += (_, _) => _tracker.FrameRendered();

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        timer.Tick += OnTick;
        timer.Start();

        if (_opts.Headless)
        {
            var shutdown = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_opts.DurationSeconds) };
            shutdown.Tick += (_, _) =>
            {
                shutdown.Stop();
                timer.Stop();
                _tracker.WriteReportFile("EXP4_OffThread_Bound");
                Close();
            };
            shutdown.Start();
        }
    }

    private void OnTick(object? sender, object e)
    {
        _tick++;
        _tracker.BeginUpdate();
        _tracker.BeginUiBlock();

        // Recompute all 1000 VMs
        for (int i = 0; i < Count; i++)
        {
            double t = _tick * 0.1 + i;
            double val = Math.Sin(t) * Math.Cos(t * 0.7) + Math.Sqrt(Math.Abs(Math.Sin(t * 0.3)));
            _vms[i].DisplayText = $"Item {i}: {val:F2}";
        }

        _tracker.EndUiBlock();
        _tracker.EndUpdate();

        // HUD
        _hudFps.Text = $"FPS: {_tracker.CurrentFps:F0}";
        _hudUpdate.Text = $"Update: {_tracker.LastUpdateMs:F2}ms";
        _hudMemory.Text = $"Mem: {_tracker.CurrentMemoryMB}MB";
    }
}
