using Duct;
using Duct.Core;
using Duct.Flex;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct;

class RichEditBoxPage : Component
{
    public override Element Render()
    {
        var (text, setText) = UseState("Type here to edit rich text content...");
        var (charCount, setCharCount) = UseState(0);

        return ScrollView(
            VStack(16,
                PageHeader("RichEditBox", "A rich text editing control with formatting support."),

                SampleCard("Basic RichEditBox",
                    RichEditBox(text, s => { setText(s); setCharCount(s.Length); })
                        .Width(400).Height(150),
                    @"var (text, setText) = UseState(""Type here..."");\nRichEditBox(text, setText).Width(400).Height(150)"),

                SampleCard("With Character Count",
                    VStack(8,
                        RichEditBox(text, s => { setText(s); setCharCount(s.Length); })
                            .Width(400).Height(120),
                        Text($"Characters: {charCount}").Foreground(Theme.SecondaryText).FontSize(12)
                    ),
                    @"RichEditBox(text, s => { setText(s); setCharCount(s.Length); })\nText($""Characters: {charCount}"")")
            ).Margin(36, 24, 36, 36)
        );
    }
}
