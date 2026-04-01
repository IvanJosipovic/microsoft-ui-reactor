using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using static Duct.D3.Charts.D3;
using static Duct.UI;

namespace DuctD3.Gallery;

public class GroupedBarChartSample : GallerySample
{
    public override string Title => "Grouped Bar Chart";
    public override string Description => "A grouped bar chart comparing quarterly sales for three product lines side by side.";
    public override string Category => "Bars";

    public override string SourceCode => """
        var ys = new LinearScale([0, maxVal], [plotH, 0]).Nice();
        var band = BandScale.Create(categories)
            .SetRange(0, plotW).SetPaddingInner(0.2).SetPaddingOuter(0.1);
        var groupBand = BandScale.Create(seriesNames)
            .SetRange(0, band.Bandwidth).SetPaddingInner(0.05);

        D3Grid(ysScreen, left, plotW);

        for (int si = 0; si < seriesNames.Length; si++)
        {
            var fill = Brush(Palette[si]);
            for (int ci = 0; ci < categories.Length; ci++)
            {
                double x = left + band.Map(categories[ci])
                         + groupBand.Map(seriesNames[si]);
                double barH = plotH - ys.Map(values[si][ci]);
                double y = top + ys.Map(values[si][ci]);
                D3Rect(x, y, groupBand.Bandwidth, barH)
                    with { Fill = fill, RadiusX = 2, RadiusY = 2 };
            }
        }
        """;

    public override Element Render()
    {
        const double W = 700, H = 400;
        const double left = 60, top = 30, right = 120, bottom = 50;
        double plotW = W - left - right;
        double plotH = H - top - bottom;

        // Sample data — quarterly sales by product line
        string[] categories = ["Q1", "Q2", "Q3", "Q4", "FY"];
        string[] seriesNames = ["Electronics", "Clothing", "Groceries"];
        double[][] values =
        [
            [120, 85, 95, 130, 430],   // Electronics
            [75, 110, 90, 100, 375],    // Clothing
            [60, 70, 105, 80, 315],     // Groceries
        ];

        // Find max value
        double maxVal = 0;
        foreach (var row in values)
            foreach (var v in row)
                if (v > maxVal) maxVal = v;

        // Scales
        var ys = new LinearScale([0, maxVal], [plotH, 0]).Nice();
        var band = BandScale.Create(categories).SetRange(0, plotW).SetPaddingInner(0.2).SetPaddingOuter(0.1);
        var groupBand = BandScale.Create(seriesNames).SetRange(0, band.Bandwidth).SetPaddingInner(0.05);
        var ysScreen = new LinearScale(ys.Domain, [top + plotH, top]);

        // Axes
        var axisBrush = Gray(100, 180);
        double legendX = W - right + 12;
        double legendY = top + 10;

        return D3Canvas(W, H,
            [.. D3Grid(ysScreen, left, plotW),

             // Grouped bars
             .. seriesNames.SelectMany((series, si) =>
             {
                 var fill = Brush(Palette[si]);
                 return categories.Select((cat, ci) =>
                 {
                     double x = left + band.Map(cat) + groupBand.Map(series);
                     double barH = plotH - ys.Map(values[si][ci]);
                     double y = top + ys.Map(values[si][ci]);
                     return D3Rect(x, y, groupBand.Bandwidth, barH) with { Fill = fill, RadiusX = 2, RadiusY = 2 };
                 });
             }),

             D3Line(left, top + plotH, left + plotW, top + plotH) with { Stroke = axisBrush, StrokeThickness = 1 },
             D3Line(left, top, left, top + plotH) with { Stroke = axisBrush, StrokeThickness = 1 },
             .. ysScreen.Ticks(5).Select(t =>
                 D3TextRight(0, ysScreen.Map(t) - 7, Fmt(t), left - 6, 10, axisBrush)),

             // X axis labels
             .. categories.Select((cat, i) =>
                 D3Text(left + band.Map(cat) + band.Bandwidth / 2 - 10, top + plotH + 8, cat, 10, axisBrush)),

             // Legend
             .. seriesNames.SelectMany((name, i) => new Element[]
             {
                 D3Rect(legendX, legendY + i * 22, 14, 14) with { Fill = Brush(Palette[i]), RadiusX = 2, RadiusY = 2 },
                 D3Text(legendX + 20, legendY + i * 22, name, 11, Gray(60)),
             }),

             D3Text(left, 4, "Quarterly Sales by Product Line", 13, Gray(40)),
            ]
        );
    }
}
