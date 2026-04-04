using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using PerfBench.Shared;

namespace PerfBench.Priorities.Bound;

public sealed class MainWindow : Window
{
    private const int ItemCount = 5000;
    private static readonly string[] Words = GenerateWords();

    private readonly BenchCliOptions _opts;
    private readonly BenchTracker _tracker = new();
    private readonly SearchViewModel _vm;
    private readonly TextBox _searchBox;
    private readonly TextBlock _hud;
    private readonly Stopwatch _inputSw = new();

    // Headless typing simulation
    private const string TypeSequence = "abcdef";
    private int _typeIndex;

    public MainWindow(BenchCliOptions opts)
    {
        _opts = opts;
        Title = "EXP-8 Priorities.Bound";

        // Generate 5000 items deterministically
        var rng = new Random(42);
        var allItems = new string[ItemCount];
        for (int i = 0; i < ItemCount; i++)
            allItems[i] = $"Item {i}: {Words[rng.Next(Words.Length)]}";

        _vm = new SearchViewModel(allItems);

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

        // ItemsControl bound to FilteredItems
        var listPanel = new StackPanel();
        var itemsControl = new ItemsControl
        {
            ItemsPanel = new ItemsPanelTemplate()
        };
        // We use manual binding: listen to CollectionChanged and rebuild
        // For simplicity, use StackPanel + manual bind like Direct but driven by VM
        var scrollViewer = new ScrollViewer { Content = listPanel };
        Grid.SetRow(scrollViewer, 1);
        root.Children.Add(scrollViewer);

        // Rebuild list when filtered items change
        _vm.FilteredItems.CollectionChanged += (_, _) =>
        {
            listPanel.Children.Clear();
            foreach (var item in _vm.FilteredItems)
            {
                var tb = new TextBlock
                {
                    FontSize = 10,
                    Margin = new Thickness(2)
                };
                tb.SetBinding(TextBlock.TextProperty, new Binding
                {
                    Source = item,
                    Path = new PropertyPath("Text"),
                    Mode = BindingMode.OneWay
                });
                listPanel.Children.Add(tb);
            }
        };

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

        if (_opts.Headless)
        {
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
                _tracker.WriteReportFile("EXP8_Priorities_Bound");
                Close();
            };
            shutdown.Start();
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _tracker.BeginUpdate();

        _vm.SearchText = _searchBox.Text;

        _tracker.EndUpdate();

        if (_inputSw.IsRunning)
        {
            _inputSw.Stop();
            _tracker.RecordInputLatency(_inputSw.Elapsed.TotalMilliseconds);
        }

        if (!_opts.Headless)
            _hud.Text = $"FPS: {_tracker.CurrentFps:F0}  Update: {_tracker.LastUpdateMs:F2}ms  Mem: {_tracker.CurrentMemoryMB}MB";
    }

    private static string[] GenerateWords()
    {
        return new[]
        {
            "alpha", "bravo", "charlie", "delta", "echo", "foxtrot", "golf", "hotel",
            "india", "juliet", "kilo", "lima", "mike", "november", "oscar", "papa",
            "quebec", "romeo", "sierra", "tango", "uniform", "victor", "whiskey", "xray",
            "yankee", "zulu", "able", "baker", "cast", "dodge", "easy", "fox"
        };
    }
}
