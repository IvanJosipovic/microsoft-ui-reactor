using Duct;
using Duct.Core;
using Duct.Flex;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct;

class GridPage : Component
{
    public override Element Render()
    {
        return ScrollView(
            VStack(16,
                PageHeader("Grid", "Arranges children in rows and columns with star/pixel/auto sizing."),

                SampleCard("Basic Grid",
                    Grid(
                        columns: ["*", "*", "*"],
                        rows: ["Auto", "Auto"],
                        Border(Text("1,1").Center().Padding(12)).Background("#E74C3C").Foreground("#FFFFFF").Grid(row: 0, column: 0),
                        Border(Text("1,2").Center().Padding(12)).Background("#3498DB").Foreground("#FFFFFF").Grid(row: 0, column: 1),
                        Border(Text("1,3").Center().Padding(12)).Background("#2ECC71").Foreground("#FFFFFF").Grid(row: 0, column: 2),
                        Border(Text("2,1").Center().Padding(12)).Background("#F39C12").Foreground("#FFFFFF").Grid(row: 1, column: 0),
                        Border(Text("2,2").Center().Padding(12)).Background("#9B59B6").Foreground("#FFFFFF").Grid(row: 1, column: 1),
                        Border(Text("2,3").Center().Padding(12)).Background("#1ABC9C").Foreground("#FFFFFF").Grid(row: 1, column: 2)
                    ).Width(400),
                    @"Grid(\n    columns: [""*"", ""*"", ""*""],\n    rows: [""Auto"", ""Auto""],\n    Border(Text(""1,1"")).Grid(row: 0, column: 0),\n    Border(Text(""1,2"")).Grid(row: 0, column: 1), ...\n)"),

                SampleCard("Column & Row Spanning",
                    Grid(
                        columns: ["*", "*", "*"],
                        rows: ["60", "60", "60"],
                        Border(Text("Header (spans 3)").Center().Foreground("#FFFFFF"))
                            .Background("#2C3E50").Grid(row: 0, column: 0, columnSpan: 3),
                        Border(Text("Left").Center().Foreground("#FFFFFF"))
                            .Background("#E74C3C").Grid(row: 1, column: 0),
                        Border(Text("Center (spans 2 cols)").Center().Foreground("#FFFFFF"))
                            .Background("#3498DB").Grid(row: 1, column: 1, columnSpan: 2),
                        Border(Text("Footer (spans 3)").Center().Foreground("#FFFFFF"))
                            .Background("#2C3E50").Grid(row: 2, column: 0, columnSpan: 3)
                    ).Width(400),
                    @"Border(Text(""Header"")).Grid(row: 0, column: 0, columnSpan: 3)\nBorder(Text(""Center"")).Grid(row: 1, column: 1, columnSpan: 2)"),

                SampleCard("Mixed Sizing",
                    Grid(
                        columns: ["100", "*", "2*"],
                        rows: ["Auto"],
                        Border(Text("100px").Center().Padding(8)).Background("#E67E22").Foreground("#FFFFFF").Grid(row: 0, column: 0),
                        Border(Text("1*").Center().Padding(8)).Background("#27AE60").Foreground("#FFFFFF").Grid(row: 0, column: 1),
                        Border(Text("2*").Center().Padding(8)).Background("#8E44AD").Foreground("#FFFFFF").Grid(row: 0, column: 2)
                    ).Width(400),
                    @"Grid(\n    columns: [""100"", ""*"", ""2*""], rows: [""Auto""],\n    // 100px fixed, 1 star, 2 star proportional\n)")
            ).Margin(36, 24, 36, 36)
        );
    }
}
