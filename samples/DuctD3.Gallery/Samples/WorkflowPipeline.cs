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
/// A CI/CD-style workflow pipeline where each stage is a composite WinUI control
/// (icon + title + ProgressBar/status) connected by D3 bezier links.
/// </summary>
public sealed class WorkflowPipelineSample : GallerySample
{
    public override string Title => "Workflow Pipeline";
    public override string Description =>
        "A directed workflow graph where each node is a composite WinUI panel (icon, title, progress bar) " +
        "connected by curved D3 bezier links. Shows how arbitrary XAML subtrees participate in D3 layout.";
    public override string Category => "Controls";

    public override string SourceCode => """
        // Position nodes in a DAG layout, draw bezier links,
        // then place composite WinUI panels at each node:
        var panel = new StackPanel {
            Children = {
                new FontIcon { Glyph = stage.Icon },
                new TextBlock { Text = stage.Name },
                new ProgressBar { Value = stage.Progress, Maximum = 100 },
            }
        };
        WinCanvas.SetLeft(panel, node.X);
        WinCanvas.SetTop(panel, node.Y);
        """;

    record Stage(string Name, string Icon, double Progress, string Status, int Column, int Row);

    record Edge(int From, int To);

    public override Element Render()
    {
        const double W = 780, H = 420;

        var stages = new Stage[]
        {
            new("Source",    "\uE943", 100, "Done",       0, 0),  // 0
            new("Build",    "\uE71A", 100, "Done",       1, 0),  // 1
            new("Unit Test","\uE9D5", 100, "Done",       2, 0),  // 2
            new("Lint",     "\uE71C", 100, "Done",       2, 1),  // 3
            new("Package",  "\uE7B8", 75,  "Running...", 3, 0),  // 4
            new("Staging",  "\uE753", 0,   "Pending",    4, 0),  // 5
            new("Approve",  "\uE8FB", 0,   "Pending",    4, 1),  // 6
            new("Deploy",   "\uE968", 0,   "Pending",    5, 0),  // 7
        };

        var edges = new Edge[]
        {
            new(0, 1), new(1, 2), new(1, 3),
            new(2, 4), new(3, 4), new(4, 5),
            new(4, 6), new(5, 7), new(6, 7),
        };

        double colW = 130, rowH = 140;
        double padX = 50, padY = 60;
        double cardW = 100, cardH = 80;

        return new XamlHostElement(() =>
        {
            var canvas = new WinCanvas
            {
                Width = W, Height = H,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            };

            // Compute node center positions
            double[] cx = new double[stages.Length];
            double[] cy = new double[stages.Length];
            for (int i = 0; i < stages.Length; i++)
            {
                cx[i] = padX + stages[i].Column * colW + cardW / 2;
                cy[i] = padY + stages[i].Row * rowH + cardH / 2;
            }

            // Draw bezier edges
            var edgeBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 100, 100, 100));
            foreach (var e in edges)
            {
                double x1 = cx[e.From], y1 = cy[e.From];
                double x2 = cx[e.To], y2 = cy[e.To];
                double mx = (x1 + x2) / 2;

                var pb = new PathBuilder(3);
                pb.MoveTo(x1, y1);
                pb.BezierCurveTo(mx, y1, mx, y2, x2, y2);

                canvas.Children.Add(new WinShapes.Path
                {
                    Data = PathDataParser.Parse(pb.ToString()),
                    Stroke = edgeBrush,
                    StrokeThickness = 2,
                    StrokeDashArray = [4, 3],
                });
            }

            var palette = D3Color.Category10;

            // Place composite control cards at each node
            for (int i = 0; i < stages.Length; i++)
            {
                var s = stages[i];
                var color = s.Status == "Done" ? palette[2]    // green
                          : s.Status == "Running..." ? palette[0]  // blue
                          : palette[7];                            // gray

                var colorBrush = new SolidColorBrush(
                    Windows.UI.Color.FromArgb((byte)(color.Opacity * 255), color.R, color.G, color.B));

                var icon = new FontIcon
                {
                    Glyph = s.Icon,
                    FontSize = 18,
                    Foreground = colorBrush,
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                };

                var title = new TextBlock
                {
                    Text = s.Name,
                    FontSize = 11,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };

                var progress = new ProgressBar
                {
                    Value = s.Progress,
                    Maximum = 100,
                    Width = 80,
                    Height = 4,
                    Margin = new Thickness(0, 4, 0, 0),
                };

                var status = new TextBlock
                {
                    Text = s.Status,
                    FontSize = 9,
                    Foreground = colorBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };

                var card = new Border
                {
                    Width = cardW,
                    Height = cardH,
                    CornerRadius = new CornerRadius(8),
                    BorderBrush = colorBrush,
                    BorderThickness = new Thickness(1.5),
                    Background = new SolidColorBrush(Windows.UI.Color.FromArgb(15, color.R, color.G, color.B)),
                    Padding = new Thickness(8),
                    Child = new StackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Spacing = 2,
                        Children = { icon, title, progress, status },
                    },
                };

                WinCanvas.SetLeft(card, cx[i] - cardW / 2);
                WinCanvas.SetTop(card, cy[i] - cardH / 2);
                canvas.Children.Add(card);
            }

            return canvas;
        }, _ => { })
        { TypeKey = "WorkflowPipeline" };
    }
}
