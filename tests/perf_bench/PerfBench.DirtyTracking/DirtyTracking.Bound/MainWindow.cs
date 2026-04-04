using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using PerfBench.Shared;

namespace PerfBench.DirtyTracking.Bound;

public sealed class MainWindow : Window
{
    private const int Cols = 10;
    private const int Rows = 20;
    private const int Total = Cols * Rows; // 200

    private readonly BenchCliOptions _opts;
    private readonly BenchTracker _tracker = new();
    private readonly CounterViewModel[] _vms = new CounterViewModel[Total];
    private readonly Random _rng = new(42);
    private readonly DispatcherTimer _timer;
    private readonly TextBlock _hud;

    public MainWindow(BenchCliOptions opts)
    {
        _opts = opts;
        Title = "EXP-1 DirtyTracking.Bound";

        var root = new Grid();

        // Build the data grid
        var dataGrid = new Grid();
        for (int c = 0; c < Cols; c++)
            dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int r = 0; r < Rows; r++)
            dataGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        for (int i = 0; i < Total; i++)
        {
            _vms[i] = new CounterViewModel(i);
            var tb = new TextBlock
            {
                FontSize = 10,
                Margin = new Thickness(2)
            };
            tb.SetBinding(TextBlock.TextProperty, new Binding
            {
                Source = _vms[i],
                Path = new PropertyPath("DisplayText"),
                Mode = BindingMode.OneWay
            });
            Grid.SetColumn(tb, i % Cols);
            Grid.SetRow(tb, i / Cols);
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
                _tracker.WriteReportFile("EXP1_DirtyTracking_Bound");
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
        _vms[idx].Increment();

        _tracker.EndUpdate();

        if (!_opts.Headless)
            _hud.Text = $"FPS: {_tracker.CurrentFps:F0}  Update: {_tracker.LastUpdateMs:F2}ms  Mem: {_tracker.CurrentMemoryMB}MB";
    }
}
