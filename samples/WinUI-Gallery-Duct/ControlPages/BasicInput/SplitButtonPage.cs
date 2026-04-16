using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct.ControlPages.BasicInput;

class SplitButtonPage: Component
{
    public override Element Render()
    {
        var (lastAction, setLastAction) = UseState("None");
        var (isToggled, setIsToggled) = UseState(false);

        return ScrollView(VStack(16,
            PageHeader("SplitButton", "A button with two parts: a primary action and a flyout menu."),

            SampleCard("Basic SplitButton",
                VStack(8,
                    SplitButton("Send", () => setLastAction("Send clicked"), MenuItems(
                        MenuItem("Send now", () => setLastAction("Send now")),
                        MenuItem("Schedule", () => setLastAction("Schedule")))),
                    Text($"Last action: {lastAction}").Foreground(Theme.SecondaryText)),
                sourceCode: @"
SplitButton(""Send"", () => setLastAction(""Send clicked""), MenuItems(
    MenuItem(""Send now"", () => setLastAction(""Send now"")),
    MenuItem(""Schedule"", () => setLastAction(""Schedule""))))
"),

            SampleCard("Toggle SplitButton",
                VStack(8,
                    ToggleSplitButton("Bold", isToggled, v => setIsToggled(v), MenuItems(
                        MenuItem("Italic"),
                        MenuItem("Underline"))),
                    Text($"Toggled: {isToggled}").Foreground(Theme.SecondaryText)),
                sourceCode: @"
ToggleSplitButton(""Bold"", isToggled, v => setIsToggled(v), MenuItems(
    MenuItem(""Italic""),
    MenuItem(""Underline"")))
")
        ).Margin(36, 24, 36, 36));
    }
}
