using Duct;
using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.D3.Charts.D3;
using static Duct.UI;

namespace DuctD3.Gallery;

public sealed class SunburstSample : GallerySample
{
    public override string Title => "Sunburst";
    public override string Description =>
        "A sunburst chart showing disk usage as nested angular slices. Uses PartitionLayout " +
        "with ToPolar and ArcGenerator to render concentric rings of a hierarchy.";
    public override string Category => "Hierarchies";

    public override string SourceCode => """
        var partition = PartitionLayout.Create<DiskNode>()
            .Size(totalAngleWidth, totalHeightNorm);
        var root = partition.Layout(data, n => n.Children, n => n.Size);
        var arc = new ArcGenerator();

        D3Canvas(W, H,
            [..allNodes.Where(n => n.Parent != null)
                .Select(node => {
                    var (sa, ea, ir, or) = node.ToPolar(...);
                    return D3PathTranslated(arc.Generate(sa, ea, 0, ir, or),
                        cx, cy, Gray(255), fill, 1);
                }),
             ..labels]
        )
        """;

    record DiskNode(string Name, double Size = 0, DiskNode[]? Children = null);

    public override Element Render()
    {
        const double W = 500, H = 500;
        double cx = W / 2, cy = H / 2;
        double maxRadius = Math.Min(W, H) / 2 - 10;

        // Disk usage hierarchy (sizes in MB)
        var data = new DiskNode("C:\\", 0, [
            new("Users", 0, [
                new("Documents", 0, [
                    new("Reports", 450),
                    new("Photos", 1200),
                    new("Projects", 800),
                ]),
                new("Downloads", 0, [
                    new("Installers", 600),
                    new("Media", 900),
                ]),
                new("AppData", 0, [
                    new("Cache", 350),
                    new("Logs", 120),
                    new("Config", 80),
                ]),
            ]),
            new("Program Files", 0, [
                new("VS Code", 400),
                new("Office", 1500),
                new("Browser", 300),
                new("Games", 2200),
            ]),
            new("Windows", 0, [
                new("System32", 1800),
                new("WinSxS", 1200),
                new("Temp", 400),
            ]),
        ]);

        // Use PartitionLayout in polar coordinates
        double totalAngleWidth = 1;
        double totalHeightNorm = 1;

        var partition = PartitionLayout.Create<DiskNode>().Size(totalAngleWidth, totalHeightNorm);
        var root = partition.Layout(data, n => n.Children, n => n.Size);

        var arc = new ArcGenerator();

        // Collect all nodes
        var allNodes = new List<PartitionNode<DiskNode>>();
        CollectPartition(root, allNodes);

        return D3Canvas(W, H,
            [.. allNodes
                .Where(node => node.Parent != null)
                .Where(node =>
                {
                    var (startAngle, endAngle, _, _) =
                        node.ToPolar(totalAngleWidth, totalHeightNorm, maxRadius);
                    return endAngle - startAngle >= 0.005;
                })
                .SelectMany(node =>
                {
                    var (startAngle, endAngle, innerRadius, outerRadius) =
                        node.ToPolar(totalAngleWidth, totalHeightNorm, maxRadius);

                    int colorIdx = GetTopBranch(node);
                    double opacity = 0.9 - node.Depth * 0.15;
                    var fill = Brush(Palette[colorIdx % Palette.Length], Math.Max(0.3, opacity));

                    string? pathData = arc.Generate(startAngle, endAngle, 0, innerRadius, outerRadius);

                    bool showLabel = (endAngle - startAngle) > 0.15 && node.Children.Count == 0;
                    double midAngle = (startAngle + endAngle) / 2 - Math.PI / 2;
                    double midR = (innerRadius + outerRadius) / 2;
                    double lx = cx + Math.Cos(midAngle) * midR;
                    double ly = cy + Math.Sin(midAngle) * midR;

                    return (Element[])
                    [
                        .. (pathData != null ? new[] { D3PathTranslated(pathData, cx, cy, Gray(255), fill, 1) } : Array.Empty<Element>()),
                        .. (showLabel ? new Element[] { (Text(node.Data.Name) with { FontSize = 8 })
                                .Foreground(Gray(30)).Width(40)
                                .Set(tb => tb.TextAlignment = TextAlignment.Center)
                                .Canvas(lx - 20, ly - 6) } : Array.Empty<Element>()),
                    ];
                }),
             (Text("Disk") with { FontSize = 12 })
                 .Foreground(Brush("#333333"))
                 .Width(40)
                 .Set(tb => tb.TextAlignment = TextAlignment.Center)
                 .Canvas(cx - 20, cy - 7),
            ]
        );
    }

    static int GetTopBranch(PartitionNode<DiskNode> node)
    {
        var current = node;
        while (current.Parent != null && current.Parent.Parent != null)
            current = current.Parent;
        if (current.Parent == null) return 0;
        return current.Parent.Children.IndexOf(current);
    }

    static void CollectPartition(PartitionNode<DiskNode> node, List<PartitionNode<DiskNode>> list)
    {
        list.Add(node);
        foreach (var child in node.Children) CollectPartition(child, list);
    }
}
