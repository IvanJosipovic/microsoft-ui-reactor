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
/// A tidy tree where each node is a live WinUI control matching its type —
/// Button, ToggleSwitch, Slider, TextBox, CheckBox — laid out by D3's
/// Reingold-Tilford algorithm.
/// </summary>
public sealed class ComponentHierarchySample : GallerySample
{
    public override string Title => "Component Hierarchy";
    public override string Description =>
        "A tidy tree layout where each node is a real WinUI control (Button, ToggleSwitch, Slider, etc.) " +
        "positioned by D3's Reingold-Tilford algorithm. Demonstrates hosting interactive controls inside a D3 graph.";
    public override string Category => "Controls";

    public override string SourceCode => """
        var layout = TreeLayout.Create<CtrlNode>().Size(640, 380);
        var root = layout.Hierarchy(data, n => n.Children);
        layout.Layout(root);

        // Draw bezier links, then place a live WinUI control at each node
        foreach (var node in root.Descendants())
        {
            var ctrl = node.Data.Kind switch {
                "Button"       => new Button { Content = node.Data.Name },
                "ToggleSwitch" => new ToggleSwitch { OnContent = "On", OffContent = "Off" },
                "Slider"       => new Slider { Width = 80, Minimum = 0, Maximum = 100, Value = 50 },
                _ => new TextBlock { Text = node.Data.Name },
            };
            WinCanvas.SetLeft(ctrl, node.X - 40);
            WinCanvas.SetTop(ctrl, node.Y - 12);
            canvas.Children.Add(ctrl);
        }
        """;

    record CtrlNode(string Name, string Kind, CtrlNode[]? Children = null);

    public override Element Render()
    {
        var data = new CtrlNode("App", "Header", [
            new("Navigation", "Header", [
                new("Home", "Button"),
                new("Settings", "Button"),
                new("Dark Mode", "ToggleSwitch"),
            ]),
            new("Content", "Header", [
                new("Search", "TextBox"),
                new("Volume", "Slider"),
                new("Accept Terms", "CheckBox"),
            ]),
            new("Actions", "Header", [
                new("Submit", "Button"),
                new("Reset", "Button"),
            ]),
        ]);

        const double W = 750, H = 460;

        return new XamlHostElement(() =>
        {
            var canvas = new WinCanvas
            {
                Width = W, Height = H,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            };

            var layout = TreeLayout.Create<CtrlNode>().Size(W - 80, H - 80);
            var root = layout.Hierarchy(data, n => n.Children);
            layout.Layout(root);

            var nodes = root.Descendants().ToList();
            var linkBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(120, 100, 100, 100));

            // Draw links
            foreach (var node in nodes)
            {
                foreach (var child in node.Children)
                {
                    double x1 = 40 + node.X, y1 = 40 + node.Y;
                    double x2 = 40 + child.X, y2 = 40 + child.Y;
                    double my = (y1 + y2) / 2;

                    var pb = new PathBuilder(3);
                    pb.MoveTo(x1, y1);
                    pb.BezierCurveTo(x1, my, x2, my, x2, y2);

                    var path = new WinShapes.Path
                    {
                        Data = PathDataParser.Parse(pb.ToString()),
                        Stroke = linkBrush,
                        StrokeThickness = 1.5,
                    };
                    canvas.Children.Add(path);
                }
            }

            var palette = D3Color.Category10;

            // Place WinUI controls at each node
            foreach (var node in nodes)
            {
                double nx = 40 + node.X, ny = 40 + node.Y;
                bool isLeaf = node.Children.Count == 0;

                FrameworkElement ctrl = node.Data.Kind switch
                {
                    "Button" => new Button
                    {
                        Content = node.Data.Name,
                        FontSize = 11,
                        Padding = new Thickness(10, 4, 10, 4),
                        Background = new SolidColorBrush(Windows.UI.Color.FromArgb(
                            (byte)(palette[0].Opacity * 255), palette[0].R, palette[0].G, palette[0].B)),
                    },
                    "ToggleSwitch" => new ToggleSwitch
                    {
                        OnContent = "On",
                        OffContent = "Off",
                        MinWidth = 0,
                        FontSize = 10,
                    },
                    "Slider" => new Slider
                    {
                        Width = 90,
                        Minimum = 0,
                        Maximum = 100,
                        Value = 50,
                        Height = 32,
                    },
                    "TextBox" => new TextBox
                    {
                        PlaceholderText = node.Data.Name,
                        Width = 90,
                        FontSize = 11,
                        Padding = new Thickness(6, 4, 6, 4),
                    },
                    "CheckBox" => new CheckBox
                    {
                        Content = node.Data.Name,
                        FontSize = 10,
                        MinWidth = 0,
                    },
                    _ => MakeHeaderBadge(node.Data.Name, palette),
                };

                WinCanvas.SetLeft(ctrl, nx - (isLeaf ? 45 : 30));
                WinCanvas.SetTop(ctrl, ny - 14);
                canvas.Children.Add(ctrl);
            }

            return canvas;
        }, _ => { })
        { TypeKey = "ComponentHierarchy" };
    }

    static Border MakeHeaderBadge(string text, D3Color[] palette)
    {
        var color = palette[3 % palette.Length];
        return new Border
        {
            Child = new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                Padding = new Thickness(10, 4, 10, 4),
            },
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(
                (byte)(color.Opacity * 255), color.R, color.G, color.B)),
            CornerRadius = new CornerRadius(4),
        };
    }
}
