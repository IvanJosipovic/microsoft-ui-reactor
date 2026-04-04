using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PerfBench.Shared;

namespace PerfBench.Journal.Direct;

public sealed class MainWindow : Window
{
    private const int Columns = 80;
    private const int Rows = 60;
    private const int Count = Columns * Rows; // 4800

    private readonly BenchCliOptions _opts;
    private readonly BenchTracker _tracker = new();
    private readonly TextBlock[] _cells = new TextBlock[Count];
    private readonly Random _rng = new(42);

    private static readonly SolidColorBrush BrushRed = new(Colors.OrangeRed);
    private static readonly SolidColorBrush BrushGreen = new(Colors.LimeGreen);
    private static readonly SolidColorBrush BrushWhite = new(Colors.White);

    private readonly TextBlock _hudFps = new();
    private readonly TextBlock _hudUpdate = new();
    private readonly TextBlock _hudMemory = new();
    private int _tick;

    public MainWindow(BenchCliOptions opts)
    {
        _opts = opts;
        Title = "EXP-5 Journal Direct";
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
            var tb = new TextBlock { FontSize = 8, Padding = new Thickness(0) };
            Grid.SetColumn(tb, i % Columns);
            Grid.SetRow(tb, i / Columns);
            grid.Children.Add(tb);
            _cells[i] = tb;
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
                _tracker.WriteReportFile("EXP5_Journal_Direct");
                Close();
            };
            shutdown.Start();
        }
    }

    private void OnTick(object? sender, object e)
    {
        _tick++;
        _tracker.BeginUpdate();

        int mutations = 0;
        double updatePercent = _opts.Percent / 100.0;

        for (int i = 0; i < Count; i++)
        {
            if (_rng.NextDouble() < updatePercent)
            {
                _cells[i].Text = $"{_tick % 1000:D3}";
                mutations++;
                _cells[i].Foreground = (_tick + i) % 2 == 0 ? BrushRed : BrushGreen;
                mutations++;
            }
        }

        _tracker.RecordMutations(mutations);
        _tracker.EndUpdate();

        // HUD
        _hudFps.Text = $"FPS: {_tracker.CurrentFps:F0}";
        _hudUpdate.Text = $"Update: {_tracker.LastUpdateMs:F2}ms";
        _hudMemory.Text = $"Mem: {_tracker.CurrentMemoryMB}MB";
    }
}
