using Duct;
using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using static Duct.D3.Charts.D3;
using static Duct.UI;
using WinCanvas = Microsoft.UI.Xaml.Controls.Canvas;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

/// <summary>
/// An animated horizontal bar chart that races through yearly data —
/// bars grow, shrink, and re-sort as the year advances. Uses D3Ease
/// for smooth interpolation and a DispatcherQueue timer to drive frames.
/// </summary>
public sealed class BarChartRaceSample : GallerySample
{
    public override string Title => "Bar Chart Race";
    public override string Description =>
        "An animated bar chart race where horizontal bars grow, shrink, and re-sort over time. " +
        "Uses D3Ease.CubicInOut for smooth transitions and DispatcherQueue for frame timing.";
    public override string Category => "Animation";

    public override string SourceCode => """
        // Timer advances the year, interpolates bar widths + positions
        timer = DispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(60);
        timer.Tick += (_, _) => {
            progress += 0.04;
            if (progress >= 1) { progress = 0; yearIdx++; }
            double t = D3Ease.Cubic(progress);
            // Interpolate bar width and Y position
            bar.Width = D3Interpolate.Number(oldW, newW)(t);
        };
        """;

    static readonly string[] Countries = ["USA", "China", "Japan", "Germany", "India", "UK", "France", "Brazil"];

    static readonly double[][] YearData =
    [
        [20.5, 14.7, 5.1, 3.8, 2.9, 2.8, 2.7, 1.9],  // 2018
        [21.4, 14.3, 5.1, 3.9, 2.9, 2.8, 2.7, 1.8],   // 2019
        [20.9, 14.7, 5.0, 3.8, 2.7, 2.7, 2.6, 1.4],   // 2020
        [23.0, 17.7, 5.0, 4.2, 3.2, 3.2, 2.9, 1.6],   // 2021
        [25.5, 18.0, 4.2, 4.1, 3.4, 3.1, 2.8, 1.9],   // 2022
        [27.4, 17.8, 4.2, 4.5, 3.7, 3.3, 3.0, 2.2],   // 2023
    ];

    static readonly string[] Years = ["2018", "2019", "2020", "2021", "2022", "2023"];

    public override Element Render()
    {
        const double W = 700, H = 420;
        const double left = 80, top = 40, right = 40, bottom = 20;
        double plotW = W - left - right;
        double plotH = H - top - bottom;
        double barH = plotH / Countries.Length * 0.75;
        double barGap = plotH / Countries.Length;

        return new XamlHostElement(() =>
        {
            var canvas = new WinCanvas
            {
                Width = W, Height = H,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            };

            var palette = D3Color.Category10;

            // Pre-create elements for animation
            var bars = new WinShapes.Rectangle[Countries.Length];
            var labels = new TextBlock[Countries.Length];
            var valueLabels = new TextBlock[Countries.Length];
            var yearLabel = new TextBlock
            {
                Text = Years[0],
                FontSize = 48,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(40, 100, 100, 100)),
            };
            WinCanvas.SetLeft(yearLabel, W - 160);
            WinCanvas.SetTop(yearLabel, H - 80);
            canvas.Children.Add(yearLabel);

            // Title
            var title = new TextBlock
            {
                Text = "GDP by Country (Trillions USD)",
                FontSize = 14,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 50, 50, 50)),
            };
            WinCanvas.SetLeft(title, 12);
            WinCanvas.SetTop(title, 8);
            canvas.Children.Add(title);

            // Sort initial data
            int[] order = Enumerable.Range(0, Countries.Length).OrderByDescending(i => YearData[0][i]).ToArray();
            double maxVal = YearData.SelectMany(y => y).Max();
            var xScale = new LinearScale([0, maxVal + 2], [0, plotW]);

            for (int rank = 0; rank < Countries.Length; rank++)
            {
                int ci = order[rank];
                var color = palette[ci % palette.Length];
                var brush = new SolidColorBrush(
                    Windows.UI.Color.FromArgb((byte)(color.Opacity * 255), color.R, color.G, color.B));

                double bw = xScale.Map(YearData[0][ci]);
                double by = top + rank * barGap;

                bars[ci] = new WinShapes.Rectangle
                {
                    Width = Math.Max(0, bw),
                    Height = barH,
                    Fill = brush,
                    RadiusX = 3,
                    RadiusY = 3,
                };
                WinCanvas.SetLeft(bars[ci], left);
                WinCanvas.SetTop(bars[ci], by);
                canvas.Children.Add(bars[ci]);

                labels[ci] = new TextBlock
                {
                    Text = Countries[ci],
                    FontSize = 11,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 50, 50, 50)),
                    TextAlignment = TextAlignment.Right,
                    Width = left - 10,
                };
                WinCanvas.SetLeft(labels[ci], 0);
                WinCanvas.SetTop(labels[ci], by + barH / 2 - 8);
                canvas.Children.Add(labels[ci]);

                valueLabels[ci] = new TextBlock
                {
                    Text = $"${YearData[0][ci]:F1}T",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 80, 80, 80)),
                };
                WinCanvas.SetLeft(valueLabels[ci], left + bw + 6);
                WinCanvas.SetTop(valueLabels[ci], by + barH / 2 - 7);
                canvas.Children.Add(valueLabels[ci]);
            }

            // Animation state
            int yearIdx = 0;
            double progress = 0;
            int[] prevOrder = order.ToArray();
            int[] nextOrder = order.ToArray();
            double[] prevValues = YearData[0].ToArray();
            double[] nextValues = YearData[0].ToArray();

            void AdvanceYear()
            {
                prevOrder = nextOrder.ToArray();
                prevValues = nextValues.ToArray();
                yearIdx = (yearIdx + 1) % YearData.Length;
                nextValues = YearData[yearIdx].ToArray();
                nextOrder = Enumerable.Range(0, Countries.Length)
                    .OrderByDescending(i => nextValues[i]).ToArray();
            }

            // First advance
            AdvanceYear();

            var timer = canvas.DispatcherQueue.CreateTimer();
            timer.Interval = TimeSpan.FromMilliseconds(50);
            timer.Tick += (_, _) =>
            {
                progress += 0.03;
                if (progress >= 1.0)
                {
                    progress = 0;
                    AdvanceYear();
                }

                double t = D3Ease.Cubic(progress);
                yearLabel.Text = Years[yearIdx];

                // Compute rank positions for prev and next
                double[] prevY = new double[Countries.Length];
                double[] nextY = new double[Countries.Length];
                for (int rank = 0; rank < Countries.Length; rank++)
                {
                    prevY[prevOrder[rank]] = top + rank * barGap;
                    nextY[nextOrder[rank]] = top + rank * barGap;
                }

                for (int ci = 0; ci < Countries.Length; ci++)
                {
                    double val = D3Interpolate.Number(prevValues[ci], nextValues[ci])(t);
                    double bw = xScale.Map(val);
                    double by = D3Interpolate.Number(prevY[ci], nextY[ci])(t);

                    bars[ci].Width = Math.Max(0, bw);
                    WinCanvas.SetLeft(bars[ci], left);
                    WinCanvas.SetTop(bars[ci], by);

                    WinCanvas.SetTop(labels[ci], by + barH / 2 - 8);

                    valueLabels[ci].Text = $"${val:F1}T";
                    WinCanvas.SetLeft(valueLabels[ci], left + bw + 6);
                    WinCanvas.SetTop(valueLabels[ci], by + barH / 2 - 7);
                }
            };
            timer.Start();

            canvas.Unloaded += (_, _) => timer.Stop();

            return canvas;
        }, _ => { })
        { TypeKey = "BarChartRace" };
    }
}
