using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using PerfBench.Shared;

namespace PerfBench.PropertyDiff.Bound;

public sealed class MainWindow : Window
{
    private const int Cols = 80;
    private const int Rows = 60;
    private const int Total = Cols * Rows; // 4800

    private readonly BenchCliOptions _opts;
    private readonly BenchTracker _tracker = new();
    private readonly CellViewModel[] _vms = new CellViewModel[Total];
    private readonly Random _rng = new(42);
    private readonly DispatcherTimer _timer;
    private readonly TextBlock _hud;

    public MainWindow(BenchCliOptions opts)
    {
        _opts = opts;
        Title = "EXP-2 PropertyDiff.Bound";

        var root = new Grid();

        var dataGrid = new Grid();
        for (int c = 0; c < Cols; c++)
            dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int r = 0; r < Rows; r++)
            dataGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        for (int i = 0; i < Total; i++)
        {
            _vms[i] = new CellViewModel(i);
            var tb = new TextBlock
            {
                FontSize = 8,
                Margin = new Thickness(1)
            };
            tb.SetBinding(TextBlock.TextProperty, new Binding
            {
                Source = _vms[i],
                Path = new PropertyPath("DisplayText"),
                Mode = BindingMode.OneWay
            });
            tb.SetBinding(TextBlock.ForegroundProperty, new Binding
            {
                Source = _vms[i],
                Path = new PropertyPath("CellBrush"),
                Mode = BindingMode.OneWay
            });
            Grid.SetColumn(tb, i % Cols);
            Grid.SetRow(tb, i / Cols);
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
                _tracker.WriteReportFile("EXP2_PropertyDiff_Bound");
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
            _vms[idx].Update(newVal);
        }

        _tracker.EndUpdate();

        if (!_opts.Headless)
            _hud.Text = $"FPS: {_tracker.CurrentFps:F0}  Update: {_tracker.LastUpdateMs:F2}ms  Mem: {_tracker.CurrentMemoryMB}MB";
    }
}
