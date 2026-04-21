using Microsoft.UI.Reactor.Core;

namespace Microsoft.UI.Reactor.Charting.Accessibility;

/// <summary>
/// Manages virtual keyboard focus for interactive charts. The chart root is a single
/// focusable Canvas; the navigator holds <c>{seriesIndex, pointIndex}</c> state and
/// renders a double-ring focus overlay at the current point.
/// </summary>
internal static class ChartKeyboardNavigator
{
    /// <summary>
    /// Wraps <paramref name="chartElement"/> with keyboard navigation support.
    /// The chart's <see cref="IChartAccessibilityData"/> is used to determine
    /// series/point bounds.
    /// </summary>
    internal static Element Wrap(
        Element chartElement,
        IChartAccessibilityData chartData,
        double chartWidth,
        double chartHeight,
        bool disableKeyboard,
        ChartKeyboardOptions options)
    {
        if (disableKeyboard)
            return chartElement;

        return new FuncElement(ctx =>
        {
            var (focusState, setFocusState) = ctx.UseState(new FocusState(0, 0, false));

            var series = chartData.Series;
            int seriesCount = series.Count;
            if (seriesCount == 0)
                return chartElement;

            int maxPoints = 0;
            for (int i = 0; i < seriesCount; i++)
            {
                if (series[i].Points.Count > maxPoints)
                    maxPoints = series[i].Points.Count;
            }

            if (maxPoints == 0)
                return chartElement;

            // Clamp focus to valid bounds
            int si = Math.Clamp(focusState.SeriesIndex, 0, seriesCount - 1);
            int pi = Math.Clamp(focusState.PointIndex, 0, series[si].Points.Count - 1);

            void HandleKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
            {
                var key = e.Key;
                int newSi = si, newPi = pi;
                bool handled = true;
                bool activate = false;

                switch (key)
                {
                    // ← / → : previous / next point in current series
                    case global::Windows.System.VirtualKey.Left:
                        newPi = Math.Max(0, pi - 1);
                        activate = true;
                        break;
                    case global::Windows.System.VirtualKey.Right:
                        newPi = Math.Min(series[si].Points.Count - 1, pi + 1);
                        activate = true;
                        break;

                    // ↑ / ↓ : switch to adjacent series
                    case global::Windows.System.VirtualKey.Up:
                        newSi = Math.Max(0, si - 1);
                        newPi = Math.Min(pi, series[newSi].Points.Count - 1);
                        activate = true;
                        break;
                    case global::Windows.System.VirtualKey.Down:
                        newSi = Math.Min(seriesCount - 1, si + 1);
                        newPi = Math.Min(pi, series[newSi].Points.Count - 1);
                        activate = true;
                        break;

                    // Home / End
                    case global::Windows.System.VirtualKey.Home:
                        if (IsCtrlPressed()) { newSi = 0; newPi = 0; }
                        else newPi = 0;
                        activate = true;
                        break;
                    case global::Windows.System.VirtualKey.End:
                        if (IsCtrlPressed())
                        {
                            newSi = seriesCount - 1;
                            newPi = series[newSi].Points.Count - 1;
                        }
                        else
                        {
                            newPi = series[si].Points.Count - 1;
                        }
                        activate = true;
                        break;

                    // Enter / Space : invoke
                    case global::Windows.System.VirtualKey.Enter:
                    case global::Windows.System.VirtualKey.Space:
                        options.OnPointInvoke?.Invoke(si, pi);
                        break;

                    // Esc : deactivate focus indicator / leave chart
                    case global::Windows.System.VirtualKey.Escape:
                        setFocusState(new FocusState(si, pi, false));
                        break;

                    default:
                        handled = false;
                        break;
                }

                if (handled)
                {
                    bool hasFocus = activate || focusState.HasFocus;
                    if (newSi != si || newPi != pi || hasFocus != focusState.HasFocus)
                        setFocusState(new FocusState(newSi, newPi, hasFocus));
                    e.Handled = true;
                }
            }

            // Build focus indicator overlay when active
            Element? focusOverlay = null;
            if (focusState.HasFocus && si < seriesCount && pi < series[si].Points.Count)
            {
                focusOverlay = BuildFocusIndicator(
                    chartData, si, pi, chartWidth, chartHeight, seriesCount, maxPoints);
            }

            // Wrap chart in a focusable Grid with the keyboard handler
            var wrappedChart = chartElement
                .IsTabStop(true)
                .OnKeyDown(HandleKeyDown);

            if (focusOverlay is null)
                return wrappedChart;

            // Overlay the focus ring using a layered Grid
            return Factories.Grid(
                ["*"], ["*"],
                wrappedChart,
                focusOverlay.Opacity(1.0)
            );
        });
    }

    /// <summary>
    /// Builds a double-ring focus indicator overlay positioned at the given point.
    /// Inner ring: 1px dark stroke. Outer ring: 1px light stroke.
    /// Guarantees 3:1 contrast against any chart background (WCAG 2.4.13).
    /// </summary>
    private static Element BuildFocusIndicator(
        IChartAccessibilityData chartData,
        int seriesIndex, int pointIndex,
        double chartWidth, double chartHeight,
        int seriesCount, int maxPoints)
    {
        // Compute approximate position based on chart geometry
        double plotLeft = 40, plotTop = 20;
        double plotWidth = chartWidth - 60;
        double plotHeight = chartHeight - 50;

        double x = maxPoints > 1
            ? plotLeft + (double)pointIndex / (maxPoints - 1) * plotWidth
            : plotLeft + plotWidth / 2;

        double y = seriesCount > 1
            ? plotTop + (double)seriesIndex / (seriesCount - 1) * plotHeight
            : plotTop + plotHeight / 2;

        const double outerRadius = 14;
        const double innerRadius = 12;

        // Double-ring for 3:1 contrast on any background
        var darkBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            D3Dsl.IsForcedColors
                ? Microsoft.UI.Colors.White
                : global::Windows.UI.Color.FromArgb(255, 0, 0, 0));
        var lightBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            D3Dsl.IsForcedColors
                ? Microsoft.UI.Colors.Cyan
                : global::Windows.UI.Color.FromArgb(255, 255, 255, 255));

        // Use D3Circle for the rings (no fill, stroke only)
        var outerRing = D3Dsl.D3Circle(x, y, outerRadius) with
        {
            Stroke = lightBrush,
            StrokeThickness = 1,
        };
        var innerRing = D3Dsl.D3Circle(x, y, innerRadius) with
        {
            Stroke = darkBrush,
            StrokeThickness = 1,
        };

        return D3Dsl.D3Canvas(chartWidth, chartHeight, outerRing, innerRing)
            .AccessibilityView(Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
    }

    private static bool IsCtrlPressed()
    {
        var state = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(
            global::Windows.System.VirtualKey.Control);
        return (state & global::Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;
    }

    internal record FocusState(int SeriesIndex, int PointIndex, bool HasFocus);
}

/// <summary>
/// Options for keyboard navigation behavior.
/// </summary>
internal record ChartKeyboardOptions
{
    /// <summary>
    /// Called when Enter/Space is pressed on a focused point.
    /// Parameters: seriesIndex, pointIndex.
    /// </summary>
    public Action<int, int>? OnPointInvoke { get; init; }
}
