using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct.ControlPages.StatusAndInfo;

class ToolTipPage : Component
{
    public override Element Render()
    {
        return ScrollView(
            VStack(16,
                PageHeader("ToolTip",
                    "A popup that displays helpful text when hovering over an element."),

                SampleCard("Text ToolTip",
                    HStack(16,
                        Button("Hover Me").ToolTip("This is a simple tooltip"),
                        Button("Save").ToolTip("Save the current document (Ctrl+S)")
                    ),
                    @"Button(""Hover Me"").ToolTip(""This is a simple tooltip"")
Button(""Save"").ToolTip(""Save document (Ctrl+S)"")"),

                SampleCard("ToolTip on Various Controls",
                    HStack(16,
                        Text("Hover this text").Foreground(Theme.AccentText)
                            .ToolTip("Text elements can have tooltips too"),
                        CheckBox(false, label: "Enable").ToolTip("Enable the feature"),
                        ToggleSwitch(false).ToolTip("Toggle dark mode")
                    ),
                    @"Text(""Hover this text"").ToolTip(""Text tooltip"")
CheckBox(false, label: ""Enable"").ToolTip(""Enable feature"")
ToggleSwitch(false).ToolTip(""Toggle dark mode"")"),

                SampleCard("Rich ToolTip",
                    Button("Rich Tooltip").WithToolTip(
                        VStack(4,
                            Text("Detailed Info").Bold(),
                            Text("This tooltip contains multiple lines of content.")
                                .Foreground(Theme.SecondaryText).FontSize(12)
                        ).Padding(4)),
                    @"Button(""Rich Tooltip"").WithToolTip(
    VStack(4,
        Text(""Title"").Bold(),
        Text(""Description"").FontSize(12)))")
            ).Margin(36, 24, 36, 36)
        );
    }
}
