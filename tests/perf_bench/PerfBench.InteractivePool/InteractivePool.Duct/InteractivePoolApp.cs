using Duct;
using Duct.Core;
using Microsoft.UI.Xaml.Media;
using PerfBench.Shared;
using static Duct.UI;

public record InteractiveItem(string ButtonLabel, string TextValue, bool IsToggled);

public class InteractivePoolApp : Component
{
    public static BenchCliOptions Opts { get; set; } = new();

    private const int ItemCount = 500;

    private readonly BenchTracker _tracker = new();
    private double _scrollOffset;

    public override Element Render()
    {
        var (items, setItems) = UseState(
            Enumerable.Range(0, ItemCount)
                .Select(i => new InteractiveItem($"Action {i}", $"Item {i}", i % 2 == 0))
                .ToArray());

        var scrollRef = UseRef<Microsoft.UI.Xaml.Controls.ScrollViewer>(null!);

        UseEffect(() =>
        {
            _tracker.ResetGcBaseline();
            _tracker.BeginMount();

            CompositionTarget.Rendering += (_, _) => _tracker.FrameRendered();

            // Mark mount complete after first render
            var mountTimer = new Microsoft.UI.Xaml.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            mountTimer.Tick += (_, _) =>
            {
                mountTimer.Stop();
                _tracker.EndMount();
            };
            mountTimer.Start();

            // Programmatic scroll at 16ms
            var scrollTimer = new Microsoft.UI.Xaml.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            scrollTimer.Tick += (_, _) =>
            {
                _scrollOffset += 20;
                var sv = scrollRef.Current;
                if (sv != null)
                {
                    sv.ChangeView(null, _scrollOffset, null, true);
                }
            };
            scrollTimer.Start();

            if (Opts.Headless)
            {
                var shutdown = new Microsoft.UI.Xaml.DispatcherTimer
                    { Interval = TimeSpan.FromSeconds(Opts.DurationSeconds) };
                shutdown.Tick += (_, _) =>
                {
                    shutdown.Stop();
                    scrollTimer.Stop();
                    _tracker.WriteReportFile("EXP6_InteractivePool_Duct");
                    Microsoft.UI.Xaml.Application.Current.Exit();
                };
                shutdown.Start();
            }

            return () => scrollTimer.Stop();
        });

        var rows = items.Select((item, i) =>
            HStack(8,
                Button(item.ButtonLabel, () => { }),
                TextField(item.TextValue, val =>
                {
                    var copy = (InteractiveItem[])items.Clone();
                    copy[i] = item with { TextValue = val };
                    setItems(copy);
                }),
                ToggleSwitch(item.IsToggled, val =>
                {
                    var copy = (InteractiveItem[])items.Clone();
                    copy[i] = item with { IsToggled = val };
                    setItems(copy);
                })
            ).Padding(2)
        ).ToArray();

        return VStack(
            HStack(
                Text($"FPS: {_tracker.CurrentFps:F0}").Width(120),
                Text($"Update: {_tracker.LastUpdateMs:F2}ms").Width(150),
                Text($"Mem: {_tracker.CurrentMemoryMB}MB").Width(150)
            ).Padding(4),
            ScrollView(
                VStack(rows)
            ).Set(sv => scrollRef.Current = sv).Height(800)
        );
    }
}
