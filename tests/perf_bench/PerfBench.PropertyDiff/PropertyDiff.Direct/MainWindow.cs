using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PerfBench.Shared;

namespace PerfBench.PropertyDiff.Direct;

public sealed class MainWindow : Window
{
    private const int Cols = 80;
    private const int Rows = 60;
    private const int Total = Cols * Rows; // 4800

    private readonly BenchCliOptions _opts;
    private readonly BenchTracker _tracker = new();
    private readonly TextBlock[] _cells = new TextBlock[Total];
    private readonly double[] _values = new double[Total];
    private readonly Random _rng = new(42);
    private readonly DispatcherTimer _timer;
    private readonly TextBlock _hud;

    private static readonly SolidColorBrush GreenBrush = new(Microsoft.UI.Colors.Green);
    private static readonly SolidColorBrush RedBrush = new(Microsoft.UI.Colors.Red);

    public MainWindow(BenchCliOptions opts)
    {
        _opts = opts;
        Title = "EXP-2 PropertyDiff.Direct";

        var root = new Grid();

        var dataGrid = new Grid();
        for (int c = 0; c < Cols; c++)
            dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int r = 0; r < Rows; r++)
            dataGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        for (int i = 0; i < Total; i++)
        {
            var tb = new TextBlock
            {
                Text = $"Cell {i}: 0.00",
                FontSize = 8,
                Foreground = GreenBrush,
                Margin = new Thickness(1)
            };
            Grid.SetColumn(tb, i % Cols);
            Grid.SetRow(tb, i / Cols);
            _cells[i] = tb;
            dataGrid.Children.Add(tb);
        }

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
                _tracker.WriteReportFile("EXP2_PropertyDiff_Direct");
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

        int updateCount = (int)(Total * _opts.Percent / 100.0);
        for (int n = 0; n < updateCount; n++)
        {
            int idx = _rng.Next(Total);
            double newVal = _rng.NextDouble() * 100.0;
            _values[idx] = newVal;
            _cells[idx].Text = $"Cell {idx}: {newVal:F2}";
            _cells[idx].Foreground = newVal >= 50.0 ? GreenBrush : RedBrush;
        }

        _tracker.EndUpdate();

        if (!_opts.Headless)
            _hud.Text = $"FPS: {_tracker.CurrentFps:F0}  Update: {_tracker.LastUpdateMs:F2}ms  Mem: {_tracker.CurrentMemoryMB}MB";
    }
}
