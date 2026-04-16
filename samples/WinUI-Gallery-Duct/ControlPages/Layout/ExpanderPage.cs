using Duct;
using Duct.Core;
using Duct.Flex;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct;

class ExpanderPage : Component
{
    public override Element Render()
    {
        var (expanded1, setExpanded1) = UseState(true);
        var (expanded2, setExpanded2) = UseState(false);
        var (dirIndex, setDirIndex) = UseState(0);

        var direction = dirIndex == 0 ? ExpandDirection.Down : ExpandDirection.Up;

        return ScrollView(
            VStack(16,
                PageHeader("Expander", "Expands and collapses to show or hide content."),

                SampleCard("Basic Expander",
                    VStack(8,
                        Expander("Settings", VStack(4,
                            Text("Option 1: Enabled"),
                            Text("Option 2: Auto-save"),
                            Text("Option 3: Dark mode")
                        ), expanded1, setExpanded1).Width(350),

                        Expander("Advanced", VStack(4,
                            Text("Cache size: 256 MB"),
                            Text("Thread count: 4"),
                            Text("Log level: Debug")
                        ), expanded2, setExpanded2).Width(350)
                    ),
                    @"Expander(""Settings"", VStack(\n    Text(""Option 1: Enabled""), ...\n), expanded, setExpanded)"),

                SampleCard("Direction Control",
                    Expander("Expand Direction", VStack(4,
                        Text("This content appears based on the direction setting."),
                        Text("Try switching between Down and Up.")
                    ), true).Direction(direction).Width(350),
                    @"Expander(""Header"", content, true)\n    .Direction(ExpandDirection.Up)",
                    OptionPanel(
                        Text("Direction"),
                        ComboBox(new[] { "Down", "Up" }, dirIndex, setDirIndex)
                    ))
            ).Margin(36, 24, 36, 36)
        );
    }
}
