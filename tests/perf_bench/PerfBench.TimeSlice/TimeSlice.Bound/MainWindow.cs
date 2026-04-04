using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using PerfBench.Shared;

namespace PerfBench.TimeSlice.Bound;

public sealed class MainWindow : Window
{
    private const int ItemCount = 2000;
    private const double BallSize = 30;

    private readonly BenchCliOptions _opts;
    private readonly BenchTracker _tracker = new();
    private readonly TextBlock _hud;

    // Bouncing ball state
    private readonly Ellipse _ball;
    private readonly Canvas _canvas;
    private double _ballX = 50;
    private double _ballY = 50;
    private double _ballDx = 3;
    private double _ballDy = 2;
    private bool _mounted;

    public MainWindow(BenchCliOptions opts)
    {
        _opts = opts;
        Title = "EXP-7 TimeSlice.Bound";

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(200) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Canvas for bouncing ball animation
        _canvas = new Canvas
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.DarkSlateGray)
        };
        _ball = new Ellipse
        {
            Width = BallSize,
            Height = BallSize,
            Fill = new SolidColorBrush(Microsoft.UI.Colors.OrangeRed)
        };
        Canvas.SetLeft(_ball, _ballX);
        Canvas.SetTop(_ball, _ballY);
        _canvas.Children.Add(_ball);
        Grid.SetRow(_canvas, 0);
        root.Children.Add(_canvas);

        // ScrollViewer + StackPanel for 2000 bound TextBlocks
        var panel = new StackPanel();
        var scrollViewer = new ScrollViewer { Content = panel };
        Grid.SetRow(scrollViewer, 1);
        root.Children.Add(scrollViewer);

        // HUD
        _hud = new TextBlock
        {
            FontSize = 14,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Yellow),
            Margin = new Thickness(8)
        };
        Grid.SetRow(_hud, 2);
        root.Children.Add(_hud);

        Content = root;

        _tracker.ResetGcBaseline();
        CompositionTarget.Rendering += OnRendering;

        // Mount 2000 bound elements on Activated
        Activated += (_, _) =>
        {
            if (_mounted) return;
            _mounted = true;

            _tracker.BeginMount();
            for (int i = 0; i < ItemCount; i++)
            {
                var vm = new TimeSliceItemViewModel(i);
                var tb = new TextBlock
                {
                    FontSize = 10,
                    Margin = new Thickness(2)
                };
                tb.SetBinding(TextBlock.TextProperty, new Binding
                {
                    Source = vm,
                    Path = new PropertyPath("DisplayText"),
                    Mode = BindingMode.OneWay
                });
                panel.Children.Add(tb);
            }
            _tracker.EndMount();

            if (_opts.Headless)
            {
                var shutdown = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_opts.DurationSeconds) };
                shutdown.Tick += (_, _) =>
                {
                    shutdown.Stop();
                    CompositionTarget.Rendering -= OnRendering;
                    _tracker.WriteReportFile("EXP7_TimeSlice_Bound");
                    Close();
                };
                shutdown.Start();
            }
        };
    }

    private void OnRendering(object? sender, object e)
    {
        _tracker.FrameRendered();

        double canvasW = _canvas.ActualWidth > 0 ? _canvas.ActualWidth : 400;
        double canvasH = _canvas.ActualHeight > 0 ? _canvas.ActualHeight : 200;

        _ballX += _ballDx;
        _ballY += _ballDy;

        if (_ballX <= 0 || _ballX + BallSize >= canvasW) _ballDx = -_ballDx;
        if (_ballY <= 0 || _ballY + BallSize >= canvasH) _ballDy = -_ballDy;

        _ballX = Math.Clamp(_ballX, 0, canvasW - BallSize);
        _ballY = Math.Clamp(_ballY, 0, canvasH - BallSize);

        Canvas.SetLeft(_ball, _ballX);
        Canvas.SetTop(_ball, _ballY);

        if (!_opts.Headless)
            _hud.Text = $"FPS: {_tracker.CurrentFps:F0}  Block: {_tracker.LongestFrameBlockMs:F1}ms  Drops: {_tracker.AnimationDrops}  Mem: {_tracker.CurrentMemoryMB}MB";
    }
}
