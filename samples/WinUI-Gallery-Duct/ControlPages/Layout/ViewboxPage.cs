using Duct;
using Duct.Core;
using Duct.Flex;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct;

class ViewboxPage : Component
{
    public override Element Render()
    {
        var (size, setSize) = UseState(150.0);

        return ScrollView(
            VStack(16,
                PageHeader("Viewbox", "Scales its child content to fill available space."),

                SampleCard("Scaling Content",
                    Border(
                        Viewbox(
                            VStack(4,
                                Text("Hello").Bold(),
                                Text("Scaled content")
                            )
                        )
                    ).Size(size, size).Background(Theme.SubtleFill).CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft),
                    @"Viewbox(\n    VStack(4, Text(""Hello"").Bold(), Text(""Scaled""))\n)",
                    OptionPanel(
                        Text($"Container size: {(int)size}px"),
                        Slider(size, 50, 300, setSize)
                    )),

                SampleCard("Viewbox Comparison",
                    HStack(16,
                        VStack(4,
                            Text("100x100").ApplyStyle("CaptionTextBlockStyle"),
                            Border(Viewbox(Text("ABC").Bold()))
                                .Size(100, 100).Background(Theme.SubtleFill).CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft)
                        ),
                        VStack(4,
                            Text("150x80").ApplyStyle("CaptionTextBlockStyle"),
                            Border(Viewbox(Text("ABC").Bold()))
                                .Size(150, 80).Background(Theme.SubtleFill).CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft)
                        ),
                        VStack(4,
                            Text("60x150").ApplyStyle("CaptionTextBlockStyle"),
                            Border(Viewbox(Text("ABC").Bold()))
                                .Size(60, 150).Background(Theme.SubtleFill).CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft)
                        )
                    ),
                    @"// Same content at different sizes:\nBorder(Viewbox(Text(""ABC""))).Size(100, 100)\nBorder(Viewbox(Text(""ABC""))).Size(150, 80)")
            ).Margin(36, 24, 36, 36)
        );
    }
}
