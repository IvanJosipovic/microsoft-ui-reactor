using Duct;
using Duct.Core;
using Duct.Flex;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct;

class BorderPage : Component
{
    public override Element Render()
    {
        var (radius, setRadius) = UseState(8.0);
        var (thickness, setThickness) = UseState(2.0);

        return ScrollView(
            VStack(16,
                PageHeader("Border", "Draws a border, background, and corner radius around its child."),

                SampleCard("Basic Border",
                    Border(Text("Content inside a border").Padding(16))
                        .WithBorder(Theme.CardStroke, thickness)
                        .CornerRadius(radius)
                        .Background(Theme.SubtleFill),
                    @"Border(Text(""Content"").Padding(16))\n    .WithBorder(Theme.CardStroke)\n    .CornerRadius(8)\n    .Background(Theme.SubtleFill)",
                    OptionPanel(
                        Text("Corner Radius"),
                        Slider(radius, 0, 32, setRadius),
                        Text("Border Thickness"),
                        Slider(thickness, 0, 8, setThickness)
                    )),

                SampleCard("Colored Borders",
                    HStack(12,
                        Border(Text("Red").Center().Padding(12)).WithBorder("#FF4444", 2).CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft),
                        Border(Text("Green").Center().Padding(12)).WithBorder("#44AA44", 2).CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft),
                        Border(Text("Blue").Center().Padding(12)).WithBorder("#4444FF", 2).CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft)
                    ),
                    @"HStack(12,\n    Border(Text(""Red"")).WithBorder(""#FF4444"", 2),\n    Border(Text(""Green"")).WithBorder(""#44AA44"", 2)\n)")
            ).Margin(36, 24, 36, 36)
        );
    }
}
