using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using PerfBench.Shared;

namespace PerfBench.Allocation.Bound;

public sealed class MainWindow : Window
{
    private const int Cols = 80;
    private const int Rows = 60;
    private const int Total = Cols * Rows; // 4800

    private readonly BenchCliOptions _opts;
    private readonly BenchTracker _tracker = new();
    private readonly AllocationCellViewModel[] _vms = new AllocationCellViewModel[Total];
    private readonly DispatcherTimer _timer;
    private readonly TextBlock _hud;

    public MainWindow(BenchCliOptions opts)
    {
        _opts = opts;
        Title = "EXP-10 Allocation.Bound";

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Build the 80x60 grid
        var dataGrid = new Grid();
        for (int c = 0; c < Cols; c++)
            dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int r = 0; r < Rows; r++)
            dataGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        for (int i = 0; i < Total; i++)
        {
            _vms[i] = new AllocationCellViewModel();
            var tb = new TextBlock
            {
                FontSize = 8,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
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

        Grid.SetRow(dataGrid, 0);
        root.Children.Add(dataGrid);

        // HUD
        _hud = new TextBlock
        {
            FontSize = 14,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Yellow),
            Margin = new Thickness(8)
        };
        Grid.SetRow(_hud, 1);
        root.Children.Add(_hud);

        Content = root;

        _tracker.ResetGcBaseline();
        CompositionTarget.Rendering += (_, _) => _tracker.FrameRendered();

        // 30Hz update timer - updates ALL 4800 VMs every tick
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
                _tracker.WriteReportFile("EXP10_Allocation_Bound");
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

        // Update ALL 4800 VMs (100% update rate)
        for (int i = 0; i < Total; i++)
            _vms[i].Increment();

        _tracker.EndUpdate();

        if (!_opts.Headless)
            _hud.Text = $"FPS: {_tracker.CurrentFps:F0}  Update: {_tracker.LastUpdateMs:F2}ms  Mem: {_tracker.CurrentMemoryMB}MB";
    }
}
