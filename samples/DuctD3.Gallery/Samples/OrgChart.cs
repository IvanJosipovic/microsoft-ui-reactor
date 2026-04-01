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
/// An org chart where each node is a "person card" with an avatar circle,
/// name, role, and a View button — laid out by D3's tree layout.
/// </summary>
public sealed class OrgChartSample : GallerySample
{
    public override string Title => "Org Chart";
    public override string Description =>
        "An organizational chart using D3 tree layout where each node is a rich person card " +
        "with avatar, name, role, and an interactive button. Shows mixing data-driven layout with WinUI controls.";
    public override string Category => "Controls";

    public override string SourceCode => """
        var layout = TreeLayout.Create<Person>().Size(660, 360);
        var root = layout.Hierarchy(ceo, p => p.Reports);
        layout.Layout(root);

        // Each node is a Border containing avatar, name, role, and a Button
        var card = new Border {
            Child = new StackPanel { Children = {
                avatar, nameBlock, roleBlock,
                new Button { Content = "View" }
            }}
        };
        WinCanvas.SetLeft(card, node.X - cardW / 2);
        WinCanvas.SetTop(card, node.Y - cardH / 2);
        """;

    record Person(string Name, string Role, string Initials, Person[]? Reports = null);

    public override Element Render()
    {
        const double W = 1000, H = 600;

        var ceo = new Person("Alex Chen", "CEO", "AC", [
            new("Jordan Lee", "VP Engineering", "JL", [
                new("Sam Park", "Tech Lead", "SP", [
                    new("Riley Kim", "Developer", "RK"),
                    new("Morgan Yu", "Developer", "MY"),
                ]),
                new("Casey Diaz", "Tech Lead", "CD", [
                    new("Quinn Brown", "Developer", "QB"),
                ]),
            ]),
            new("Taylor Swift", "VP Product", "TS", [
                new("Dana White", "PM", "DW"),
                new("Jamie Fox", "Designer", "JF"),
            ]),
            new("Robin Gray", "VP Sales", "RG", [
                new("Avery Stone", "Account Exec", "AS"),
                new("Blake Reed", "Account Exec", "BR"),
            ]),
        ]);

        return new XamlHostElement(() =>
        {
            var canvas = new WinCanvas
            {
                Width = W, Height = H,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            };

            var layout = TreeLayout.Create<Person>().Size(W - 100, H - 120);
            var root = layout.Hierarchy(ceo, p => p.Reports);
            layout.Layout(root);

            var nodes = root.Descendants().ToList();
            var linkBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(80, 100, 100, 100));
            var palette = D3Color.Category10;

            // Draw links
            foreach (var node in nodes)
            {
                foreach (var child in node.Children)
                {
                    double x1 = 50 + node.X, y1 = 60 + node.Y;
                    double x2 = 50 + child.X, y2 = 60 + child.Y;
                    double my = (y1 + y2) / 2;

                    var pb = new PathBuilder(3);
                    pb.MoveTo(x1, y1);
                    pb.BezierCurveTo(x1, my, x2, my, x2, y2);

                    canvas.Children.Add(new WinShapes.Path
                    {
                        Data = PathDataParser.Parse(pb.ToString()),
                        Stroke = linkBrush,
                        StrokeThickness = 1.5,
                    });
                }
            }

            // Place person cards
            double cardW = 100, cardH = 88;
            foreach (var node in nodes)
            {
                double nx = 50 + node.X, ny = 60 + node.Y;
                bool isLeaf = node.Children.Count == 0;
                int depth = 0;
                var p = node.Parent;
                while (p != null) { depth++; p = p.Parent; }

                var colorIdx = depth % palette.Length;
                var color = palette[colorIdx];
                var colorBrush = new SolidColorBrush(
                    Windows.UI.Color.FromArgb((byte)(color.Opacity * 255), color.R, color.G, color.B));

                // Avatar circle with initials
                var avatar = new Border
                {
                    Width = 28, Height = 28,
                    CornerRadius = new CornerRadius(14),
                    Background = colorBrush,
                    Child = new TextBlock
                    {
                        Text = node.Data.Initials,
                        FontSize = 11,
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    HorizontalAlignment = HorizontalAlignment.Center,
                };

                var nameBlock = new TextBlock
                {
                    Text = node.Data.Name,
                    FontSize = 10,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = cardW - 12,
                };

                var roleBlock = new TextBlock
                {
                    Text = node.Data.Role,
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 100, 100, 100)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = cardW - 12,
                };

                var card = new Border
                {
                    Width = cardW,
                    Height = cardH,
                    CornerRadius = new CornerRadius(8),
                    BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(60, 100, 100, 100)),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 252, 252, 253)),
                    Padding = new Thickness(6),
                    Child = new StackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Spacing = 3,
                        Children = { avatar, nameBlock, roleBlock },
                    },
                };

                WinCanvas.SetLeft(card, nx - cardW / 2);
                WinCanvas.SetTop(card, ny - cardH / 2);
                canvas.Children.Add(card);
            }

            return canvas;
        }, _ => { })
        { TypeKey = "OrgChart" };
    }
}
