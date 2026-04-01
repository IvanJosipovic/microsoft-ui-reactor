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

namespace DuctD3.Gallery;

/// <summary>
/// A treemap where each rectangle contains a WinUI ListView showing the
/// child items for that category. D3 sizes the outer regions; WinUI
/// provides scrollable drill-down inside each cell.
/// </summary>
public sealed class NestedListExplorerSample : GallerySample
{
    public override string Title => "Nested List Explorer";
    public override string Description =>
        "A treemap where each rectangle hosts a WinUI ListView showing child items. " +
        "D3's squarify layout sizes categories; standard WinUI controls provide scrollable drill-down.";
    public override string Category => "Controls";

    public override string SourceCode => """
        var treemap = TreemapLayout.Create<Category>()
            .Size(W, H).SetPadding(4).SetPaddingInner(4);
        var root = treemap.Hierarchy(data, n => n.Subs, n => n.Size);
        treemap.Layout(root);

        // Each top-level cell becomes a Border + ListView
        foreach (var folder in root.Children)
        {
            var list = new ListView();
            foreach (var leaf in folder.Leaves())
                list.Items.Add(leaf.Data.Name);
            WinCanvas.SetLeft(border, folder.X0);
            WinCanvas.SetTop(border, folder.Y0);
        }
        """;

    record CatalogItem(string Name, double Size = 0, CatalogItem[]? Subs = null);

    public override Element Render()
    {
        const double W = 700, H = 460;

        var data = new CatalogItem("Store", 0, [
            new("Electronics", 0, [
                new("Laptop", 320),
                new("Phone", 280),
                new("Tablet", 190),
                new("Headphones", 80),
                new("Monitor", 250),
                new("Keyboard", 45),
                new("Mouse", 30),
            ]),
            new("Clothing", 0, [
                new("Jacket", 120),
                new("Jeans", 85),
                new("T-Shirt", 40),
                new("Sneakers", 110),
                new("Hat", 25),
            ]),
            new("Books", 0, [
                new("Fiction", 60),
                new("Science", 55),
                new("History", 45),
                new("Art", 35),
            ]),
            new("Home", 0, [
                new("Sofa", 400),
                new("Table", 200),
                new("Lamp", 60),
                new("Rug", 90),
                new("Shelf", 75),
                new("Mirror", 50),
            ]),
        ]);

        return new XamlHostElement(() =>
        {
            var canvas = new WinCanvas
            {
                Width = W, Height = H,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            };

            var treemap = TreemapLayout.Create<CatalogItem>()
                .Size(W, H)
                .SetPadding(4)
                .SetPaddingInner(4);
            var root = treemap.Hierarchy(data, n => n.Subs, n => n.Size);
            treemap.Layout(root);

            var palette = D3Color.Category10;

            for (int ci = 0; ci < root.Children.Count; ci++)
            {
                var folder = root.Children[ci];
                double fw = folder.Width;
                double fh = folder.Height;
                if (fw < 10 || fh < 10) continue;

                var color = palette[ci % palette.Length];
                var colorBrush = new SolidColorBrush(
                    Windows.UI.Color.FromArgb((byte)(color.Opacity * 255), color.R, color.G, color.B));
                var bgBrush = new SolidColorBrush(
                    Windows.UI.Color.FromArgb(15, color.R, color.G, color.B));

                // Header
                var header = new TextBlock
                {
                    Text = folder.Data.Name,
                    FontSize = 12,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = colorBrush,
                    Margin = new Thickness(8, 6, 8, 2),
                };

                // List of leaf items
                var listView = new ListView
                {
                    SelectionMode = ListViewSelectionMode.Single,
                    IsItemClickEnabled = true,
                    Padding = new Thickness(0),
                    Margin = new Thickness(4, 0, 4, 4),
                };

                foreach (var leaf in folder.Leaves())
                {
                    var row = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        Children =
                        {
                            new TextBlock { Text = leaf.Data.Name, FontSize = 11, VerticalAlignment = VerticalAlignment.Center },
                            new TextBlock
                            {
                                Text = $"${leaf.Data.Size:N0}",
                                FontSize = 10,
                                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(160, 100, 100, 100)),
                                VerticalAlignment = VerticalAlignment.Center,
                            },
                        },
                    };
                    listView.Items.Add(row);
                }

                var cell = new Border
                {
                    Width = fw,
                    Height = fh,
                    CornerRadius = new CornerRadius(6),
                    BorderBrush = colorBrush,
                    BorderThickness = new Thickness(1.5),
                    Background = bgBrush,
                    Child = new StackPanel
                    {
                        Children = { header, listView },
                    },
                };

                WinCanvas.SetLeft(cell, folder.X0);
                WinCanvas.SetTop(cell, folder.Y0);
                canvas.Children.Add(cell);
            }

            return canvas;
        }, _ => { })
        { TypeKey = "NestedListExplorer" };
    }
}
