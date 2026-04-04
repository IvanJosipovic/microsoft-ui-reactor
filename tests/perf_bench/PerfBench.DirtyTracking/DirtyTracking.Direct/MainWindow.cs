using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PerfBench.Shared;

namespace PerfBench.DirtyTracking.Direct;

public sealed class MainWindow : Window
{
    private const int Cols = 10;
    private const int Rows = 20;
    private const int Total = Cols * Rows; // 200

    private readonly BenchCliOptions _opts;
    private readonly BenchTracker _tracker = new();
    private readonly TextBlock[] _cells = new TextBlock[Total];
    private readonly int[] _values = new int[Total];
    private readonly Random _rng = new(42);
    private readonly DispatcherTimer _timer;
    private readonly TextBlock _hud;

    public MainWindow(BenchCliOptions opts)
    {
        _opts = opts;
        Title = "EXP-1 DirtyTracking.Direct";

        var root = new Grid();

        // Build the data grid
        var dataGrid = new Grid();
        for (int c = 0; c < Cols; c++)
            dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int r = 0; r < Rows; r++)
            dataGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        for (int i = 0; i < Total; i++)
        {
            var tb = new TextBlock
            {
                Text = $"Counter {i}: 0",
                FontSize = 10,
                Margin = new Thickness(2)
            };
            Grid.SetColumn(tb, i % Cols);
            Grid.SetRow(tb, i / Cols);
            _cells[i] = tb;
            dataGrid.Children.Add(tb);
        }

        // HUD
        _hud = new TextBlock
        {
            FontSize = 14,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Yellow),
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(8)
        };

        root.Children.Add(dataGrid);
        root.Children.Add(_hud);
        Content = root;

        _tracker.ResetGcBaseline();
        CompositionTarget.Rendering += (_, _) => _tracker.FrameRendered();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _timer.Tick += OnTick;

        if (_opts.Headless)
        {
            _timer.Start();
            var shutdown = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_opts.DurationSeconds) };
            shutdown.Tick += (_, _) =>
            {
                shutdown.Stop();
                _timer.Stop();
                _tracker.WriteReportFile("EXP1_DirtyTracking_Direct");
                Close();
            };
            shutdown.Start();
        }
        else
        {
            _timer.Start();
        }
    }

    private void OnTick(object? sender, object e)
    {
        _tracker.BeginUpdate();

        int idx = _rng.Next(Total);
        _values[idx]++;
        _cells[idx].Text = $"Counter {idx}: {_values[idx]}";

        _tracker.EndUpdate();

        if (!_opts.Headless)
            _hud.Text = $"FPS: {_tracker.CurrentFps:F0}  Update: {_tracker.LastUpdateMs:F2}ms  Mem: {_tracker.CurrentMemoryMB}MB";
    }
}
