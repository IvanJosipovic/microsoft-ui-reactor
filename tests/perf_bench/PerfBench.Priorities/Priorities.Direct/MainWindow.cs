using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PerfBench.Shared;

namespace PerfBench.Priorities.Direct;

public sealed class MainWindow : Window
{
    private const int ItemCount = 5000;
    private static readonly string[] Words = GenerateWords();

    private readonly BenchCliOptions _opts;
    private readonly BenchTracker _tracker = new();
    private readonly string[] _allItems;
    private readonly TextBox _searchBox;
    private readonly StackPanel _listPanel;
    private readonly TextBlock _hud;
    private readonly Stopwatch _inputSw = new();

    // Headless typing simulation
    private const string TypeSequence = "abcdef";
    private int _typeIndex;

    public MainWindow(BenchCliOptions opts)
    {
        _opts = opts;
        Title = "EXP-8 Priorities.Direct";

        // Generate 5000 items deterministically
        var rng = new Random(42);
        _allItems = new string[ItemCount];
        for (int i = 0; i < ItemCount; i++)
            _allItems[i] = $"Item {i}: {Words[rng.Next(Words.Length)]}";

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Search TextBox
        _searchBox = new TextBox
        {
            PlaceholderText = "Search...",
            Margin = new Thickness(8)
        };
        _searchBox.TextChanged += OnSearchTextChanged;
        Grid.SetRow(_searchBox, 0);
        root.Children.Add(_searchBox);

        // List panel in ScrollViewer
        _listPanel = new StackPanel();
        var scrollViewer = new ScrollViewer { Content = _listPanel };
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
        CompositionTarget.Rendering += (_, _) => _tracker.FrameRendered();

        // Initial population
        PopulateList("");

        if (_opts.Headless)
        {
            // Simulate typing at 100ms intervals
            var typeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            typeTimer.Tick += (_, _) =>
            {
                if (_typeIndex < TypeSequence.Length)
                {
                    _typeIndex++;
                    _inputSw.Restart();
                    _searchBox.Text = TypeSequence[.._typeIndex];
                }
            };
            typeTimer.Start();

            var shutdown = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_opts.DurationSeconds) };
            shutdown.Tick += (_, _) =>
            {
                shutdown.Stop();
                typeTimer.Stop();
                _tracker.WriteReportFile("EXP8_Priorities_Direct");
                Close();
            };
            shutdown.Start();
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _tracker.BeginUpdate();

        string filter = _searchBox.Text.ToLowerInvariant();
        PopulateList(filter);

        _tracker.EndUpdate();

        if (_inputSw.IsRunning)
        {
            _inputSw.Stop();
            _tracker.RecordInputLatency(_inputSw.Elapsed.TotalMilliseconds);
        }

        if (!_opts.Headless)
            _hud.Text = $"FPS: {_tracker.CurrentFps:F0}  Update: {_tracker.LastUpdateMs:F2}ms  Mem: {_tracker.CurrentMemoryMB}MB";
    }

    private void PopulateList(string filter)
    {
        _listPanel.Children.Clear();
        foreach (var item in _allItems)
        {
            if (string.IsNullOrEmpty(filter) || item.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                _listPanel.Children.Add(new TextBlock
                {
                    Text = item,
                    FontSize = 10,
                    Margin = new Thickness(2)
                });
            }
        }
    }

    private static string[] GenerateWords()
    {
        // Deterministic word list for reproducible benchmarks
        return new[]
        {
            "alpha", "bravo", "charlie", "delta", "echo", "foxtrot", "golf", "hotel",
            "india", "juliet", "kilo", "lima", "mike", "november", "oscar", "papa",
            "quebec", "romeo", "sierra", "tango", "uniform", "victor", "whiskey", "xray",
            "yankee", "zulu", "able", "baker", "cast", "dodge", "easy", "fox"
        };
    }
}
