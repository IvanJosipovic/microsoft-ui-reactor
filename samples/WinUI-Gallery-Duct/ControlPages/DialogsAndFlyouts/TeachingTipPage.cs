using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct.ControlPages.DialogsAndFlyouts;

class TeachingTipPage : Component
{
    public override Element Render()
    {
        var (showTip, setShowTip) = UseState(false);

        return ScrollView(
            VStack(16,
                PageHeader("TeachingTip",
                    "A notification flyout for guiding users through features."),

                SampleCard("Basic TeachingTip",
                    VStack(8,
                        Button("Show Teaching Tip", () => setShowTip(true)),
                        (TeachingTip("Welcome!", "This is a helpful tip to guide you through this feature.") with
                        {
                            IsOpen = showTip,
                            OnClosed = () => setShowTip(false),
                        })
                    ),
                    @"Button(""Show Tip"", () => setShow(true)),
TeachingTip(""Welcome!"", ""Helpful guidance text."") with {
    IsOpen = show,
    OnClosed = () => setShow(false),
}"),

                SampleCard("TeachingTip (Title Only)",
                    TeachingTip("Did you know?", "You can customize the title bar!"),
                    @"TeachingTip(""Did you know?"", ""You can customize the title bar!"")")
            ).Margin(36, 24, 36, 36)
        );
    }
}
