using Duct;
using Duct.Core;
using Duct.Flex;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct;

class FlipViewPage : Component
{
    public override Element Render()
    {
        var (index, setIndex) = UseState(0);
        var colors = new[] { "#FF4444", "#44AA44", "#4444FF", "#AAAA00" };

        return ScrollView(
            VStack(16,
                PageHeader("FlipView", "Presents a collection of items one at a time with flipping navigation."),

                SampleCard("Basic FlipView",
                    FlipView(
                        colors.Select((c, i) =>
                            Border(Text($"Item {i + 1}").Center().Foreground("#FFFFFF").Bold())
                                .Background(c).Size(300, 200)
                        ).ToArray()
                    ),
                    @"FlipView(\n    Border(Text(""Item 1"")).Background(""#FF4444"").Size(300,200),\n    Border(Text(""Item 2"")).Background(""#44AA44"").Size(300,200)\n)"),

                SampleCard("Data-Driven FlipView",
                    FlipView(
                        colors,
                        c => c,
                        (c, i) => Border(
                            VStack(4,
                                Text($"Slide {i + 1}").Bold().Foreground("#FFFFFF"),
                                Text(c).Foreground("#FFFFFF")
                            ).Center()
                        ).Background(c).Size(300, 200)
                    ),
                    @"FlipView(\n    colors,\n    c => c,\n    (c, i) => Border(VStack(...)).Background(c).Size(300, 200)\n)")
            ).Margin(36, 24, 36, 36)
        );
    }
}
