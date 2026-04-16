using Duct;
using Duct.Core;
using Duct.Flex;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct;

class RelativePanelPage : Component
{
    public override Element Render()
    {
        return ScrollView(
            VStack(16,
                PageHeader("RelativePanel", "Positions children relative to each other or the panel edges."),

                SampleCard("Basic RelativePanel",
                    Border(
                        RelativePanel(
                            Border(Text("Top-Left").Center().Foreground("#FFFFFF").Padding(8))
                                .Background("#E74C3C").CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft)
                                .RelativePanel(name: "topLeft", alignLeftWithPanel: true, alignTopWithPanel: true),

                            Border(Text("Right of Red").Center().Foreground("#FFFFFF").Padding(8))
                                .Background("#3498DB").CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft)
                                .RelativePanel(name: "rightBlock", rightOf: "topLeft")
                                .Margin(8, 0, 0, 0),

                            Border(Text("Below Red").Center().Foreground("#FFFFFF").Padding(8))
                                .Background("#2ECC71").CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft)
                                .RelativePanel(name: "belowBlock", below: "topLeft")
                                .Margin(0, 8, 0, 0)
                        )
                    ).Size(400, 200).Background(Theme.SubtleFill).CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft),
                    @"RelativePanel(\n    Border(""Top-Left"").RelativePanel(name: ""topLeft"",\n        alignLeftWithPanel: true, alignTopWithPanel: true),\n    Border(""Right"").RelativePanel(name: ""r"", rightOf: ""topLeft"")\n)"),

                SampleCard("Panel Alignment",
                    Border(
                        RelativePanel(
                            Border(Text("Center").Center().Foreground("#FFFFFF").Padding(8))
                                .Background("#9B59B6").CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft)
                                .RelativePanel(name: "center",
                                    alignHorizontalCenterWithPanel: true,
                                    alignVerticalCenterWithPanel: true),

                            Border(Text("Bottom-Right").Center().Foreground("#FFFFFF").Padding(8))
                                .Background("#E67E22").CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft)
                                .RelativePanel(name: "bottomRight",
                                    alignRightWithPanel: true,
                                    alignBottomWithPanel: true)
                        )
                    ).Size(400, 200).Background(Theme.SubtleFill).CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft),
                    @"Border(""Center"").RelativePanel(name: ""center"",\n    alignHorizontalCenterWithPanel: true,\n    alignVerticalCenterWithPanel: true)")
            ).Margin(36, 24, 36, 36)
        );
    }
}
