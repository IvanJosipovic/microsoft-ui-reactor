using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;
using static WinUIGalleryReactor.SamplePageHost;

namespace WinUIGalleryReactor;

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
                            Factories.Text("Option 1: Enabled"),
                            Factories.Text("Option 2: Auto-save"),
                            Factories.Text("Option 3: Dark mode")
                        ), expanded1, setExpanded1).Width(350),

                        Expander("Advanced", VStack(4,
                            Factories.Text("Cache size: 256 MB"),
                            Factories.Text("Thread count: 4"),
                            Factories.Text("Log level: Debug")
                        ), expanded2, setExpanded2).Width(350)
                    ),
                    @"Expander(""Settings"", VStack(\n    Factories.Text(""Option 1: Enabled""), ...\n), expanded, setExpanded)"),

                SampleCard("Direction Control",
                    Expander("Expand Direction", VStack(4,
                        Factories.Text("This content appears based on the direction setting."),
                        Factories.Text("Try switching between Down and Up.")
                    ), true).Direction(direction).Width(350),
                    @"Expander(""Header"", content, true)\n    .Direction(ExpandDirection.Up)",
                    OptionPanel(
                        Factories.Text("Direction"),
                        ComboBox(new[] { "Down", "Up" }, dirIndex, setDirIndex)
                    ))
            ).Margin(36, 24, 36, 36)
        );
    }
}
