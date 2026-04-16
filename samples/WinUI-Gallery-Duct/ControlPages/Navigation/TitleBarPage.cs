using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct.ControlPages.Navigation;

class TitleBarPage : Component
{
    public override Element Render()
    {
        var (subtitle, setSubtitle) = UseState("Preview");

        return ScrollView(
            VStack(16,
                PageHeader("TitleBar",
                    "A customizable title bar for the application window."),

                SampleCard("Basic TitleBar",
                    Border(
                        TitleBar("My Application")
                    ).Background(Theme.LayerFill).CornerRadius(4).Height(48),
                    @"TitleBar(""My Application"")"),

                SampleCard("TitleBar with Subtitle",
                    VStack(8,
                        Border(
                            TitleBar("My App").Subtitle(subtitle)
                        ).Background(Theme.LayerFill).CornerRadius(4).Height(48),
                        TextField(subtitle, s => setSubtitle(s), placeholder: "Enter subtitle")
                            .Width(250)
                    ),
                    @"TitleBar(""My App"").Subtitle(""Preview"")",
                    options: OptionPanel(
                        TextField(subtitle, s => setSubtitle(s), header: "Subtitle")
                    )),

                SampleCard("TitleBar with Content",
                    Border(
                        TitleBar("Gallery") with
                        {
                            Content = HStack(8,
                                AutoSuggestBox("", _ => { }).Width(200),
                                Button("\uE713", () => { }).Width(36).Height(36)
                            ),
                        }
                    ).Background(Theme.LayerFill).CornerRadius(4).Height(48),
                    @"TitleBar(""Gallery"") with {
    Content = HStack(8,
        AutoSuggestBox("""", _ => {}).Width(200),
        Button(""⚙"", () => {}))
}")
            ).Margin(36, 24, 36, 36)
        );
    }
}
