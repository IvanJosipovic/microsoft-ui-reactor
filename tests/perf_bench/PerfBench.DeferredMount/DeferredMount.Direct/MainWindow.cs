using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PerfBench.Shared;

namespace PerfBench.DeferredMount.Direct;

public sealed class MainWindow : Window
{
    private const int TabCount = 5;
    private const int ItemsPerTab = 200;

    private readonly BenchCliOptions _opts;
    private readonly BenchTracker _tracker = new();
    private readonly StackPanel[] _tabPanels = new StackPanel[TabCount];
    private readonly Button[] _tabButtons = new Button[TabCount];
    private readonly TextBlock _hud;
    private int _activeTab;

    public MainWindow(BenchCliOptions opts)
    {
        _opts = opts;
        Title = "EXP-9 DeferredMount.Direct";

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Tab header row
        var tabHeader = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4) };
        for (int t = 0; t < TabCount; t++)
        {
            int tabIndex = t;
            var btn = new Button
            {
                Content = $"Tab {t}",
                Margin = new Thickness(2),
                MinWidth = 80
            };
            btn.Click += (_, _) => SwitchTab(tabIndex);
            _tabButtons[t] = btn;
            tabHeader.Children.Add(btn);
        }
        Grid.SetRow(tabHeader, 0);
        root.Children.Add(tabHeader);

        // Content area: all 5 tab panels stacked, visibility toggled
        var contentArea = new Grid();
        _tracker.BeginMount();
        for (int t = 0; t < TabCount; t++)
        {
            var panel = new StackPanel();
            for (int i = 0; i < ItemsPerTab; i++)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"Tab {t} - Item {i}",
                    FontSize = 10,
                    Margin = new Thickness(2)
                });
            }
            panel.Visibility = t == 0 ? Visibility.Visible : Visibility.Collapsed;
            _tabPanels[t] = panel;

            var sv = new ScrollViewer { Content = panel };
            contentArea.Children.Add(sv);
        }
        _tracker.EndMount();

        Grid.SetRow(contentArea, 1);
        root.Children.Add(contentArea);

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
        CompositionTarget.Rendering += (_, _) =>
        {
            _tracker.FrameRendered();
            if (!_opts.Headless)
                _hud.Text = $"FPS: {_tracker.CurrentFps:F0}  Block: {_tracker.LongestFrameBlockMs:F1}ms  Mem: {_tracker.CurrentMemoryMB}MB";
        };

        UpdateTabHighlight();

        if (_opts.Headless)
        {
            // After 3s, switch tabs sequentially every 1s
            int switchIndex = 1;
            var switchTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            switchTimer.Tick += (_, _) =>
            {
                switchTimer.Stop();
                var tabTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                tabTimer.Tick += (_, _) =>
                {
                    if (switchIndex < TabCount)
                    {
                        _tracker.BeginUpdate();
                        SwitchTab(switchIndex);
                        _tracker.EndUpdate();
                        switchIndex++;
                    }
                };
                tabTimer.Start();
            };
            switchTimer.Start();

            var shutdown = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_opts.DurationSeconds) };
            shutdown.Tick += (_, _) =>
            {
                shutdown.Stop();
                _tracker.WriteReportFile("EXP9_DeferredMount_Direct");
                Close();
            };
            shutdown.Start();
        }
    }

    private void SwitchTab(int tabIndex)
    {
        if (tabIndex == _activeTab) return;
        _tabPanels[_activeTab].Visibility = Visibility.Collapsed;
        _tabPanels[tabIndex].Visibility = Visibility.Visible;
        _activeTab = tabIndex;
        UpdateTabHighlight();
    }

    private void UpdateTabHighlight()
    {
        for (int i = 0; i < TabCount; i++)
        {
            _tabButtons[i].Background = i == _activeTab
                ? new SolidColorBrush(Microsoft.UI.Colors.SteelBlue)
                : null;
        }
    }
}
