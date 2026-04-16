using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct.ControlPages.Styles;

class AcrylicPage : Component
{
    public override Element Render()
    {
        var (opacity, setOpacity) = UseState(0.8);

        return ScrollView(
            VStack(16,
                PageHeader("Acrylic",
                    "A translucent material brush that creates a frosted glass effect."),

                SampleCard("Acrylic Brush",
                    VStack(8,
                        Border(
                            Text("Acrylic Background")
                                .Foreground(Theme.PrimaryText)
                                .Padding(24)
                                .FontSize(16)
                        ).Background(AcrylicBrush(
                            Windows.UI.Color.FromArgb(255, 100, 100, 200),
                            opacity))
                         .CornerRadius(8)
                         .Width(300).Height(100),
                        Border(
                            Text("Dark Acrylic")
                                .Foreground("#FFFFFF")
                                .Padding(24)
                                .FontSize(16)
                        ).Background(AcrylicBrush(
                            Windows.UI.Color.FromArgb(255, 30, 30, 30),
                            opacity))
                         .CornerRadius(8)
                         .Width(300).Height(100)
                    ),
                    @"AcrylicBrush(
    Windows.UI.Color.FromArgb(255, 100, 100, 200),
    tintOpacity: 0.8)",
                    options: OptionPanel(
                        Text($"Tint Opacity: {opacity:F2}").Foreground(Theme.SecondaryText),
                        Slider(opacity, 0, 1, v => setOpacity(v))
                    )),

                SampleCard("Acrylic Colors",
                    HStack(12,
                        Border(Text("Blue").Foreground("#FFFFFF").Padding(16))
                            .Background(AcrylicBrush(Windows.UI.Color.FromArgb(255, 0, 120, 215), 0.7))
                            .CornerRadius(8).Width(100).Height(80),
                        Border(Text("Green").Foreground("#FFFFFF").Padding(16))
                            .Background(AcrylicBrush(Windows.UI.Color.FromArgb(255, 16, 137, 62), 0.7))
                            .CornerRadius(8).Width(100).Height(80),
                        Border(Text("Red").Foreground("#FFFFFF").Padding(16))
                            .Background(AcrylicBrush(Windows.UI.Color.FromArgb(255, 200, 50, 50), 0.7))
                            .CornerRadius(8).Width(100).Height(80)
                    ),
                    @"AcrylicBrush(Color.FromArgb(255, 0, 120, 215), 0.7)")
            ).Margin(36, 24, 36, 36)
        );
    }
}
