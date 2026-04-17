using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;
using static WinUIGalleryReactor.SamplePageHost;

namespace WinUIGalleryReactor.ControlPages.Media;

class ImagePage : Component
{
    public override Element Render()
    {
        var (width, setWidth) = UseState(300.0);

        return ScrollView(
            VStack(16,
                PageHeader("Image",
                    "A control that displays an image from a file or URI."),

                SampleCard("Image from URI",
                    Image("ms-appx:///Assets/Square150x150Logo.scale-200.png")
                        .Width(width).Height(width),
                    @"Image(""ms-appx:///Assets/Square150x150Logo.scale-200.png"")
    .Width(300).Height(300)",
                    options: OptionPanel(
                        Slider(width, 50, 500, v => setWidth(v))
                    )),

                SampleCard("Image with Border",
                    Border(
                        Image("ms-appx:///Assets/Square150x150Logo.scale-200.png")
                            .Width(200).Height(200)
                    ).CornerRadius(ThemeResource.CornerRadius("OverlayCornerRadius").TopLeft)
                     .WithBorder(Theme.CardStroke),
                    @"Border(
    Image(""ms-appx:///Assets/image.png"")
        .Width(200).Height(200)
).CornerRadius(12)
 .WithBorder(Theme.CardStroke)")
            ).Margin(36, 24, 36, 36)
        );
    }
}
