using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PerfBench.Shared;

namespace PerfBench.StructuralSharing.Direct;

public sealed class MainWindow : Window
{
    private const int PanelCount = 5;
    private const int ItemsPerPanel = 50;

    private readonly BenchCliOptions _opts;
    private readonly BenchTracker _tracker = new();
    private readonly TextBlock[][] _cells;
    private readonly double[][] _values;
    private readonly Random _rng = new(42);
    private readonly DispatcherTimer _timer;
    private readonly TextBlock _hud;

    public MainWindow(BenchCliOptions opts)
    {
        _opts = opts;
        Title = "EXP-3 StructuralSharing.Direct";

        _cells = new TextBlock[PanelCount][];
        _values = new double[PanelCount][];
        for (int p = 0; p < PanelCount; p++)
        {
            _cells[p] = new TextBlock[ItemsPerPanel];
            _values[p] = new double[ItemsPerPanel];
        }

        var root = new Grid();

        // 5 panels side by side
        var panelGrid = new Grid();
        for (int p = 0; p < PanelCount; p++)
            panelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        for (int p = 0; p < PanelCount; p++)
        {
            var panel = new StackPanel { Margin = new Thickness(4) };

            var header = new TextBlock
            {
                Text = $"Panel {p}",
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            panel.Children.Add(header);

            for (int i = 0; i < ItemsPerPanel; i++)
            {
                var tb = new TextBlock
                {
                    Text = $"Item {i}: 0.00",
                    FontSize = 10,
                    Margin = new Thickness(2)
                };
                _cells[p][i] = tb;
                panel.Children.Add(tb);
            }

            Grid.SetColumn(panel, p);
            panelGrid.Children.Add(panel);
        }

        _hud = new TextBlock
        {
            FontSize = 14,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Yellow),
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(8)
        };

        root.Children.Add(panelGrid);
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
                _tracker.WriteReportFile("EXP3_StructuralSharing_Direct");
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

        int panelIdx = _rng.Next(PanelCount);
        for (int i = 0; i < ItemsPerPanel; i++)
        {
            double newVal = _rng.NextDouble() * 100.0;
            _values[panelIdx][i] = newVal;
            _cells[panelIdx][i].Text = $"Item {i}: {newVal:F2}";
        }

        _tracker.EndUpdate();

        if (!_opts.Headless)
            _hud.Text = $"FPS: {_tracker.CurrentFps:F0}  Update: {_tracker.LastUpdateMs:F2}ms  Mem: {_tracker.CurrentMemoryMB}MB";
    }
}
