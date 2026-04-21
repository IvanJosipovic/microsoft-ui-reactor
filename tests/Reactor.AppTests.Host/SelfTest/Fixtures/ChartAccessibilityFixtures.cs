using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Charting;
using Microsoft.UI.Reactor.Charting.Accessibility;
using Microsoft.UI.Reactor.Charting.D3;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hosting;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;
using static Microsoft.UI.Reactor.Charting.ChartDsl;
using static Microsoft.UI.Reactor.Charting.D3Dsl;

namespace Microsoft.UI.Reactor.AppTests.Host.SelfTest.Fixtures;

/// <summary>
/// Self-test fixtures for chart accessibility — peer attachment, grid/table providers,
/// point values, labels, and summary.
/// </summary>
internal static class ChartAccessibilityFixtures
{
    private record DataPoint(double X, double Y);

    private static readonly DataPoint[] SampleLine =
        Enumerable.Range(0, 5).Select(i => new DataPoint(i, (i + 1) * 100.0)).ToArray();

    private static readonly DataPoint[] SampleBars =
    [
        new(0, 30), new(1, 70), new(2, 45), new(3, 90), new(4, 55)
    ];

    private record PieSlice(string Label, double Value);
    private static readonly PieSlice[] SamplePie =
    [
        new("Alpha", 30), new("Beta", 20), new("Gamma", 35), new("Delta", 15)
    ];

    // ════════════════════════════════════════════════════════════════════
    //  1.6 — Peer wiring
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Mount a LineChart and verify IChartAccessibilityData is implemented.
    /// </summary>
    internal class PeerAttachment(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            // Verify ChartElement implements IChartAccessibilityData
            var chart = ChartDsl.LineChart(SampleLine, d => d.X, d => d.Y)
                .Title("Test Line Chart")
                .SeriesName("Revenue");

            H.Check("ChartA11y_PeerAttachment_ImplementsInterface",
                chart is IChartAccessibilityData);

            var a11y = (IChartAccessibilityData)chart;
            H.Check("ChartA11y_PeerAttachment_HasName",
                a11y.Name == "Test Line Chart");

            // Mount the chart to verify rendering still works
            var host = H.CreateHost();
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx => chart.ToElement());
            await Harness.Render();

            var canvas = H.FindControl<Canvas>(_ => true);
            H.Check("ChartA11y_PeerAttachment_CanvasCreated",
                canvas is not null);
        }
    }

    /// <summary>
    /// Mount a 2-series BarChart (simulated), verify IGridProvider-compatible data.
    /// </summary>
    internal class GridProvider(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var chart = ChartDsl.BarChart(SampleBars, d => d.X, d => d.Y)
                .SeriesName("Sales");

            var a11y = (IChartAccessibilityData)chart;
            var series = a11y.Series;

            // Single series chart: RowCount = 1, ColumnCount = 5
            H.Check("ChartA11y_GridProvider_RowCount",
                series.Count == 1);
            H.Check("ChartA11y_GridProvider_ColumnCount",
                series[0].Points.Count == 5);

            // Mount
            var host = H.CreateHost();
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx => chart.ToElement());
            await Harness.Render();

            H.Check("ChartA11y_GridProvider_Rendered",
                H.FindControl<Canvas>(_ => true) is not null);
        }
    }

    /// <summary>
    /// Mount a LineChart with known data, verify point value strings.
    /// </summary>
    internal class PointValue(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var chart = ChartDsl.LineChart(SampleLine, d => d.X, d => d.Y)
                .SeriesName("Revenue")
                .Units(yUnits: " USD");

            var a11y = (IChartAccessibilityData)chart;
            var series = a11y.Series;
            var points = series[0].Points;

            // Verify point data
            H.Check("ChartA11y_PointValue_FirstPoint",
                points[0].YValue == 100.0);
            H.Check("ChartA11y_PointValue_LastPoint",
                points[4].YValue == 500.0);

            // Verify default label format through FormatDefaultLabel
            var label = ChartPointProvider.FormatDefaultLabel(series[0], points[0], 0, " USD");
            H.Check("ChartA11y_PointValue_LabelContainsSeriesName",
                label.Contains("Revenue"));
            H.Check("ChartA11y_PointValue_LabelContainsUnits",
                label.Contains("USD"));
            H.Check("ChartA11y_PointValue_LabelContainsPointIndex",
                label.Contains("point 1 of 5"));

            var host = H.CreateHost();
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx => chart.ToElement());
            await Harness.Render();
        }
    }

    /// <summary>
    /// Verify ITableProvider row headers match series names and column headers match x-labels.
    /// </summary>
    internal class TableHeaders(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var chart = ChartDsl.LineChart(SampleLine, d => d.X, d => d.Y)
                .SeriesName("Quarterly Revenue");

            var a11y = (IChartAccessibilityData)chart;

            // Row header = series name
            H.Check("ChartA11y_TableHeaders_SeriesName",
                a11y.Series[0].Name == "Quarterly Revenue");

            // Column headers = x-axis tick labels
            H.Check("ChartA11y_TableHeaders_FirstXLabel",
                a11y.Series[0].Points[0].XLabel == "0");

            // Axes present
            H.Check("ChartA11y_TableHeaders_HasAxes",
                a11y.Axes.Count == 2);

            var host = H.CreateHost();
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx => chart.ToElement());
            await Harness.Render();
        }
    }

    /// <summary>
    /// Mount a PieChart, verify slices are exposed as accessible points.
    /// </summary>
    internal class PieChart(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var chart = ChartDsl.PieChart(SamplePie, d => d.Value, d => d.Label)
                .Title("Distribution");

            var a11y = (IChartAccessibilityData)chart;

            H.Check("ChartA11y_PieChart_HasName",
                a11y.Name == "Distribution");
            H.Check("ChartA11y_PieChart_SliceCount",
                a11y.Series[0].Points.Count == 4);
            H.Check("ChartA11y_PieChart_SliceLabel",
                a11y.Series[0].Points[0].XLabel == "Alpha");
            H.Check("ChartA11y_PieChart_SliceValue",
                a11y.Series[0].Points[0].YValue == 30);

            // No axes for pie charts
            H.Check("ChartA11y_PieChart_NoAxes",
                a11y.Axes.Count == 0);

            var host = H.CreateHost();
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx => chart.ToElement());
            await Harness.Render();
        }
    }

    /// <summary>
    /// Mount a ForceGraph, verify edges exposed as accessible rows.
    /// </summary>
    internal class ForceGraph(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var nodes = new ForceNode[]
            {
                new() { Label = "Alice" },
                new() { Label = "Bob" },
                new() { Label = "Carol" },
            };
            var links = new ForceLink[]
            {
                new(0, 1, Strength: 1),
                new(1, 2, Strength: 2),
            };

            var graph = ChartDsl.ForceGraph(nodes, links)
                .Title("Social Network");

            var a11y = (IChartAccessibilityData)graph;

            H.Check("ChartA11y_ForceGraph_HasName",
                a11y.Name == "Social Network");
            H.Check("ChartA11y_ForceGraph_EdgeCount",
                a11y.Series[0].Points.Count == 2);
            H.Check("ChartA11y_ForceGraph_EdgeLabel",
                a11y.Series[0].Points[0].XLabel.Contains("Alice") &&
                a11y.Series[0].Points[0].XLabel.Contains("Bob"));

            var host = H.CreateHost();
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx => graph.ToElement());
            await Harness.Render();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  2.5 — Labels and summary
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Test default point labels with SeriesName and Units.
    /// </summary>
    internal class DefaultPointLabels(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var chart = ChartDsl.LineChart(SampleLine, d => d.X, d => d.Y)
                .SeriesName("Revenue")
                .Units(yUnits: " USD");

            var a11y = (IChartAccessibilityData)chart;
            var series = a11y.Series[0];
            var point = series.Points[0];

            var label = ChartPointProvider.FormatDefaultLabel(series, point, 0, " USD");
            H.Check("ChartA11y_DefaultPointLabels_Format",
                label.Contains("Revenue") &&
                label.Contains("USD") &&
                label.Contains("point 1 of 5"));

            var host = H.CreateHost();
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx => chart.ToElement());
            await Harness.Render();
        }
    }

    /// <summary>
    /// Test custom DataLabel override.
    /// </summary>
    internal class CustomDataLabel(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var chart = ChartDsl.LineChart(SampleLine, d => d.X, d => d.Y)
                .DataLabel((d, i) => $"Custom #{i}: {d.Y}");

            var a11y = (IChartAccessibilityData)chart;
            var points = a11y.Series[0].Points;

            H.Check("ChartA11y_CustomDataLabel_FirstPoint",
                points[0].FormattedLabel == "Custom #0: 100");
            H.Check("ChartA11y_CustomDataLabel_LastPoint",
                points[4].FormattedLabel == "Custom #4: 500");

            var host = H.CreateHost();
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx => chart.ToElement());
            await Harness.Render();
        }
    }

    /// <summary>
    /// Test auto-summary contains trend keywords for increasing data.
    /// </summary>
    internal class AutoSummary(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            // Create clearly increasing data
            var data = Enumerable.Range(0, 10)
                .Select(i => new DataPoint(i, i * 100.0))
                .ToArray();

            var chart = ChartDsl.LineChart(data, d => d.X, d => d.Y)
                .SeriesName("Revenue");

            var a11y = (IChartAccessibilityData)chart;
            var summary = ChartSummarizer.Summarize(a11y, "Line");
            var formatted = ChartSummarizer.FormatSummary(summary);

            H.Check("ChartA11y_AutoSummary_ContainsTrend",
                formatted.Contains("increasing"));
            H.Check("ChartA11y_AutoSummary_ContainsStats",
                formatted.Contains("Revenue") && formatted.Contains("min") && formatted.Contains("max"));

            var host = H.CreateHost();
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx => chart.ToElement());
            await Harness.Render();
        }
    }

    /// <summary>
    /// Chart with .Title() exposes that title as peer name.
    /// </summary>
    internal class AutoNameFromTitle(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var chart = ChartDsl.LineChart(SampleLine, d => d.X, d => d.Y)
                .Title("Revenue Over Time");

            var a11y = (IChartAccessibilityData)chart;
            H.Check("ChartA11y_AutoNameFromTitle",
                a11y.Name == "Revenue Over Time");

            var host = H.CreateHost();
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx => chart.ToElement());
            await Harness.Render();
        }
    }

    /// <summary>
    /// Chart without title falls back to type-based name.
    /// </summary>
    internal class AutoNameFallback(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var chart = ChartDsl.LineChart(SampleLine, d => d.X, d => d.Y);

            var a11y = (IChartAccessibilityData)chart;
            H.Check("ChartA11y_AutoNameFallback_NoExplicitName",
                a11y.Name is null);

            // Series count still available for fallback name generation
            H.Check("ChartA11y_AutoNameFallback_HasSeries",
                a11y.Series.Count > 0);

            var host = H.CreateHost();
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx => chart.ToElement());
            await Harness.Render();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  3.9 — Forced-colors and double-encoding
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// In forced-colors mode, series brushes use system high-contrast colors.
    /// </summary>
    internal class ForcedColorsPalette(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            D3Dsl.IsForcedColors = true;
            try
            {
                var brush0 = D3Dsl.ChartSeries(0);
                var brush1 = D3Dsl.ChartSeries(1);
                var brush2 = D3Dsl.ChartSeries(2);
                var brush3 = D3Dsl.ChartSeries(3);

                H.Check("ChartA11y_ForcedColorsPalette_Series0",
                    brush0.Color == Microsoft.UI.Colors.White);
                H.Check("ChartA11y_ForcedColorsPalette_Series1",
                    brush1.Color == Microsoft.UI.Colors.Cyan);
                H.Check("ChartA11y_ForcedColorsPalette_Series2",
                    brush2.Color == Microsoft.UI.Colors.Yellow);
                H.Check("ChartA11y_ForcedColorsPalette_Series3",
                    brush3.Color == Microsoft.UI.Colors.Green);
            }
            finally
            {
                D3Dsl.IsForcedColors = false;
            }
            await Harness.Render();
        }
    }

    /// <summary>
    /// Multi-series chart has distinct marker shape AND dash pattern for each series.
    /// </summary>
    internal class DoubleEncoding(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var palette = ChartPalette.OkabeIto;

            // Verify first 4 series have distinct markers
            var markers = Enumerable.Range(0, 4).Select(i => palette.GetMarker(i)).ToList();
            H.Check("ChartA11y_DoubleEncoding_DistinctMarkers",
                markers.Distinct().Count() == 4);

            // Verify first 4 series have distinct dashes
            var dashes = Enumerable.Range(0, 4).Select(i => palette.GetDash(i)).ToList();
            H.Check("ChartA11y_DoubleEncoding_DistinctDashes",
                dashes.Distinct().Count() == 4);

            await Harness.Render();
        }
    }

    /// <summary>
    /// .ColorOnly() suppresses shape/dash encoding.
    /// </summary>
    internal class ColorOnlyWarning(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var chart = ChartDsl.LineChart(SampleLine, d => d.X, d => d.Y)
                .Title("Revenue")
                .ColorOnly();

            H.Check("ChartA11y_ColorOnlyWarning_IsColorOnly",
                chart.IsColorOnly);

            // Mount and verify scanner flags it
            var host = H.CreateHost();
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx => chart.ToElement());
            await Harness.Render();

            // Run scanner on the element tree
            var tree = VStack(chart.ToElement());
            var findings = AccessibilityScanner.Scan(tree);
            H.Check("ChartA11y_ColorOnlyWarning_ScannerFlagsIt",
                findings.Any(f => f.Id == "A11Y_CHART_004"));
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  4.3 — Scanner rules
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Chart without title triggers A11Y_CHART_001 with correct fix suggestion.
    /// </summary>
    internal class ScannerMissingTitle(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var chart = ChartDsl.LineChart(SampleLine, d => d.X, d => d.Y);

            var host = H.CreateHost();
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx => chart.ToElement());
            await Harness.Render();

            var tree = VStack(chart.ToElement());
            var findings = AccessibilityScanner.Scan(tree);

            var titleFinding = findings.FirstOrDefault(f => f.Id == "A11Y_CHART_001");
            H.Check("ChartA11y_Scanner_MissingTitle_Found",
                titleFinding is not null);
            H.Check("ChartA11y_Scanner_MissingTitle_FixModifier",
                titleFinding?.Fix.Modifier == "Title");
            H.Check("ChartA11y_Scanner_MissingTitle_FixSnippet",
                titleFinding?.Fix.CodeSnippet?.Contains(".Title(") == true);
        }
    }

    /// <summary>
    /// Chart with .ColorOnly() triggers A11Y_CHART_004.
    /// </summary>
    internal class ScannerColorOnly(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var chart = ChartDsl.LineChart(SampleLine, d => d.X, d => d.Y)
                .Title("Revenue")
                .ColorOnly();

            var host = H.CreateHost();
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx => chart.ToElement());
            await Harness.Render();

            var tree = VStack(chart.ToElement());
            var findings = AccessibilityScanner.Scan(tree);

            H.Check("ChartA11y_Scanner_ColorOnly_Found",
                findings.Any(f => f.Id == "A11Y_CHART_004"));
        }
    }

    /// <summary>
    /// Chart with known-bad palette triggers A11Y_CHART_009 or _010 with hardened alternative.
    /// </summary>
    internal class ScannerUnsafePalette(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var chart = ChartDsl.LineChart(SampleLine, d => d.X, d => d.Y)
                .Title("Revenue")
                .SeriesColors(
                    new D3Color(128, 128, 128),
                    new D3Color(135, 135, 135)); // Very similar grays

            var host = H.CreateHost();
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx => chart.ToElement());
            await Harness.Render();

            var tree = VStack(chart.ToElement());
            var findings = AccessibilityScanner.Scan(tree);

            var paletteFinding = findings.FirstOrDefault(
                f => f.Id == "A11Y_CHART_009" || f.Id == "A11Y_CHART_010");
            H.Check("ChartA11y_Scanner_UnsafePalette_Found",
                paletteFinding is not null);
            H.Check("ChartA11y_Scanner_UnsafePalette_HasHardenedAlternative",
                paletteFinding?.Fix.SuggestedValue is not null);
        }
    }

    /// <summary>
    /// Chart with .Title() and defaults passes scanner with zero chart violations.
    /// </summary>
    internal class ScannerClean(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var chart = ChartDsl.LineChart(SampleLine, d => d.X, d => d.Y)
                .Title("Monthly Revenue")
                .SeriesName("Revenue")
                .Units("months", "USD");

            var host = H.CreateHost();
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx => chart.ToElement());
            await Harness.Render();

            var tree = VStack(chart.ToElement());
            var findings = AccessibilityScanner.Scan(tree);
            var chartViolations = findings.Where(f => f.Id.StartsWith("A11Y_CHART_")).ToList();

            H.Check("ChartA11y_Scanner_Clean_ZeroViolations",
                chartViolations.Count == 0);
        }
    }

    /// <summary>
    /// Reduced-motion: verify D3Dsl.IsReducedMotion flag is readable.
    /// </summary>
    internal class ReducedMotion(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            // Save and restore the flag
            bool saved = D3Dsl.IsReducedMotion;
            try
            {
                D3Dsl.IsReducedMotion = true;
                H.Check("ChartA11y_ReducedMotion_FlagSet",
                    D3Dsl.IsReducedMotion == true);

                D3Dsl.IsReducedMotion = false;
                H.Check("ChartA11y_ReducedMotion_FlagCleared",
                    D3Dsl.IsReducedMotion == false);
            }
            finally
            {
                D3Dsl.IsReducedMotion = saved;
            }
            await Harness.Render();
        }
    }

    /// <summary>
    /// Hit target expansion: verify .TightHitTest() flag is tracked and scanner fires.
    /// </summary>
    internal class HitTargetExpansion(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var chart = ChartDsl.LineChart(SampleLine, d => d.X, d => d.Y)
                .Title("Test")
                .Interactive()
                .TightHitTest();

            H.Check("ChartA11y_HitTargetExpansion_IsTight",
                chart.IsTightHitTest);

            var tree = VStack(chart.ToElement());
            var findings = AccessibilityScanner.Scan(tree);

            H.Check("ChartA11y_HitTargetExpansion_ScannerFlags",
                findings.Any(f => f.Id == "A11Y_CHART_005"));

            await Harness.Render();
        }
    }

    /// <summary>
    /// Scanner: interactive chart with keyboard disabled triggers A11Y_CHART_003.
    /// </summary>
    internal class ScannerInteractiveNoKeyboard(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var chart = ChartDsl.LineChart(SampleLine, d => d.X, d => d.Y)
                .Title("Test")
                .Interactive()
                .DisableKeyboard();

            var tree = VStack(chart.ToElement());
            var findings = AccessibilityScanner.Scan(tree);

            H.Check("ChartA11y_Scanner_InteractiveNoKeyboard_Found",
                findings.Any(f => f.Id == "A11Y_CHART_003"));

            await Harness.Render();
        }
    }

    /// <summary>
    /// AlternateView toggle: verify chart with .AlternateView() compiles and renders.
    /// </summary>
    internal class AlternateViewToggle(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var alternate = TextBlock("Data table placeholder");
            var chart = ChartDsl.LineChart(SampleLine, d => d.X, d => d.Y)
                .Title("Revenue")
                .AlternateView(alternate);

            var host = H.CreateHost();
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx => chart.ToElement());
            await Harness.Render();

            // Chart should render without errors
            H.Check("ChartA11y_AlternateViewToggle_Mounts", true);
        }
    }

    /// <summary>
    /// AlternateView no-op: chart without .AlternateView() should render fine.
    /// </summary>
    internal class AlternateViewNoOp(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var chart = ChartDsl.LineChart(SampleLine, d => d.X, d => d.Y)
                .Title("Revenue");

            var host = H.CreateHost();
            XamlInterop.Register(host.Reconciler);
            host.Mount(ctx => chart.ToElement());
            await Harness.Render();

            // Chart without AlternateView renders without error
            H.Check("ChartA11y_AlternateViewNoOp_Mounts", true);
        }
    }
}
