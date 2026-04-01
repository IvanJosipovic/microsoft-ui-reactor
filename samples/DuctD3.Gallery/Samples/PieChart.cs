using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using static Duct.D3.Charts.D3;
using static Duct.UI;

namespace DuctD3.Gallery;

public sealed class PieChartSample : GallerySample
{
    public override string Title => "Pie Chart";
    public override string Description => "A classic pie chart showing market share across five companies, using PieGenerator and ArcGenerator.";
    public override string Category => "Radial";

    public override string SourceCode => """
        var pie = PieGenerator.Create<(string, double)>(d => d.Value)
            .SetSortValues(null);
        var arc = new ArcGenerator().SetOuterRadius(150).SetInnerRadius(0);
        var arcs = pie.Generate(data);
        D3Canvas(width, height,
            ..arcs.Select((a, i) =>
                D3PathTranslated(arc.Generate(a), cx, cy,
                    fill: Brush(Palette[i % Palette.Length])))
        )
        """;

    public override Element Render()
    {
        const double width = 700, height = 400;
        double cx = width / 2, cy = height / 2;

        var data = new (string Name, double Value)[]
        {
            ("Chrome", 65.0), ("Safari", 18.0), ("Firefox", 7.0),
            ("Edge", 5.0), ("Other", 5.0)
        };

        var pie = PieGenerator.Create<(string Name, double Value)>(d => d.Value)
            .SetSortValues(null);
        var arc = new ArcGenerator().SetOuterRadius(150).SetInnerRadius(0);
        var arcs = pie.Generate(data);

        var labelArc = new ArcGenerator().SetOuterRadius(180).SetInnerRadius(180);

        return D3Canvas(width, height,
            [.. arcs.Select((a, i) => (a, i, pathData: arc.Generate(a)))
                .Where(t => t.pathData != null)
                .SelectMany(t =>
                {
                    var (ox, oy) = labelArc.Centroid(t.a.StartAngle, t.a.EndAngle);
                    return new Element[]
                    {
                        D3PathTranslated(t.pathData!, cx, cy,
                            fill: Brush(Palette[t.i % Palette.Length]),
                            stroke: new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                            strokeWidth: 1),
                        D3Text(cx + ox - 20, cy + oy - 7,
                            $"{t.a.Data.Name} ({t.a.Data.Value}%)", 11, Brush(Palette[t.i % Palette.Length])),
                    };
                }),
             D3Text(cx - 60, 10, "Market Share", 16, Gray(40)),
            ]
        );
    }
}
