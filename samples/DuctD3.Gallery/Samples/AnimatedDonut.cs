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
/// A donut chart that smoothly animates between different datasets. Wedges
/// expand, shrink, and transition using D3Ease.ElasticOut interpolation of
/// startAngle/endAngle. A button cycles through datasets.
/// </summary>
public sealed class AnimatedDonutSample : GallerySample
{
    public override string Title => "Animated Donut";
    public override string Description =>
        "A donut chart that animates between datasets — wedges smoothly expand and shrink. " +
        "Uses D3Interpolate for angle interpolation and D3Ease for spring-like transitions.";
    public override string Category => "Animation";

    public override string SourceCode => """
        // Interpolate startAngle/endAngle between old and new pie layouts
        timer.Tick += (_, _) => {
            progress += 0.025;
            double t = D3Ease.Cubic(progress);
            for (int i = 0; i < sliceCount; i++) {
                double sa = D3Interpolate.Number(oldArcs[i].Start, newArcs[i].Start)(t);
                double ea = D3Interpolate.Number(oldArcs[i].End, newArcs[i].End)(t);
                paths[i].Data = ParsePathData(arcGen.Generate(sa, ea, padAngle));
            }
        };
        """;

    static readonly string[] Labels = ["Q1", "Q2", "Q3", "Q4"];

    static readonly double[][] Datasets =
    [
        [40, 30, 20, 10],   // Even spread
        [60, 15, 15, 10],   // Q1 dominant
        [20, 50, 20, 10],   // Q2 dominant
        [10, 10, 60, 20],   // Q3 dominant
        [25, 25, 25, 25],   // Uniform
    ];

    static readonly string[] DatasetNames =
        ["Default", "Q1 Surge", "Q2 Surge", "Q3 Surge", "Uniform"];

    record ArcState(double Start, double End);

    public override Element Render()
    {
        const double W = 400, H = 400;
        double cx = W / 2, cy = H / 2;
        const double outerR = 150, innerR = 80;
        const double padAngle = 0.03;
        const int sliceCount = 4;

        return new XamlHostElement(() =>
        {
            var canvas = new WinCanvas
            {
                Width = W, Height = H,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            };

            var palette = D3Color.Category10;
            var arcGen = new ArcGenerator().SetInnerRadius(innerR).SetOuterRadius(outerR);

            // Initial arcs
            var initArcs = PieGenerator.Generate(Datasets[0], v => v, sort: false, padAngle: padAngle);
            var oldState = initArcs.Select(a => new ArcState(a.StartAngle, a.EndAngle)).ToArray();
            var newState = oldState.ToArray();

            // Create path elements
            var paths = new WinShapes.Path[sliceCount];
            var labelBlocks = new TextBlock[sliceCount];

            for (int i = 0; i < sliceCount; i++)
            {
                var color = palette[i % palette.Length];
                var brush = new SolidColorBrush(
                    Windows.UI.Color.FromArgb((byte)(color.Opacity * 255), color.R, color.G, color.B));

                string? pd = arcGen.Generate(initArcs[i].StartAngle, initArcs[i].EndAngle, padAngle);
                paths[i] = new WinShapes.Path
                {
                    Fill = brush,
                    Stroke = new SolidColorBrush(Microsoft.UI.Colors.White),
                    StrokeThickness = 2,
                    RenderTransform = new TranslateTransform { X = cx, Y = cy },
                };
                if (pd != null)
                    paths[i].Data = PathDataParser.Parse(pd);
                canvas.Children.Add(paths[i]);

                var (lx, ly) = ArcGenerator.Centroid(initArcs[i].StartAngle, initArcs[i].EndAngle,
                    innerRadius: outerR + 20, outerRadius: outerR + 20);
                labelBlocks[i] = new TextBlock
                {
                    Text = Labels[i],
                    FontSize = 12,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = brush,
                };
                WinCanvas.SetLeft(labelBlocks[i], cx + lx - 12);
                WinCanvas.SetTop(labelBlocks[i], cy + ly - 8);
                canvas.Children.Add(labelBlocks[i]);
            }

            // Center label
            var centerLabel = new TextBlock
            {
                Text = DatasetNames[0],
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 60, 60, 60)),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Width = innerR * 1.4,
            };
            WinCanvas.SetLeft(centerLabel, cx - innerR * 0.7);
            WinCanvas.SetTop(centerLabel, cy - 10);
            canvas.Children.Add(centerLabel);

            // Navigation button
            int datasetIdx = 0;
            double progress = 1.0; // start fully resolved

            var nextButton = new Button
            {
                Content = "Next Dataset \u25B6",
                FontSize = 12,
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            WinCanvas.SetLeft(nextButton, cx - 55);
            WinCanvas.SetTop(nextButton, H - 40);

            var timer = canvas.DispatcherQueue.CreateTimer();
            timer.Interval = TimeSpan.FromMilliseconds(16);

            nextButton.Click += (_, _) =>
            {
                oldState = newState.ToArray();
                datasetIdx = (datasetIdx + 1) % Datasets.Length;
                var targetArcs = PieGenerator.Generate(Datasets[datasetIdx], v => v, sort: false, padAngle: padAngle);
                newState = targetArcs.Select(a => new ArcState(a.StartAngle, a.EndAngle)).ToArray();
                centerLabel.Text = DatasetNames[datasetIdx];
                progress = 0;
                timer.Start();
            };

            timer.Tick += (_, _) =>
            {
                progress += 0.025;
                if (progress >= 1.0)
                {
                    progress = 1.0;
                    timer.Stop();
                }

                double t = D3Ease.Cubic(progress);

                for (int i = 0; i < sliceCount; i++)
                {
                    double sa = D3Interpolate.Number(oldState[i].Start, newState[i].Start)(t);
                    double ea = D3Interpolate.Number(oldState[i].End, newState[i].End)(t);

                    string? pd = arcGen.Generate(sa, ea, padAngle);
                    if (pd != null)
                        paths[i].Data = PathDataParser.Parse(pd);

                    var (lx, ly) = ArcGenerator.Centroid(sa, ea,
                        innerRadius: outerR + 20, outerRadius: outerR + 20);
                    WinCanvas.SetLeft(labelBlocks[i], cx + lx - 12);
                    WinCanvas.SetTop(labelBlocks[i], cy + ly - 8);
                }
            };

            canvas.Children.Add(nextButton);
            canvas.Unloaded += (_, _) => timer.Stop();

            return canvas;
        }, _ => { })
        { TypeKey = "AnimatedDonut" };
    }
}
