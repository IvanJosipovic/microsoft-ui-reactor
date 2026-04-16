using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct.ControlPages.BasicInput;

class DropDownButtonPage: Component
{
    public override Element Render()
    {
        var (selected, setSelected) = UseState("None");

        return ScrollView(VStack(16,
            PageHeader("DropDownButton", "A button that displays a flyout of choices when clicked."),

            SampleCard("DropDownButton with MenuItems",
                VStack(8,
                    DropDownButton("Choose", MenuItems(
                        MenuItem("Option A", () => setSelected("Option A")),
                        MenuItem("Option B", () => setSelected("Option B")),
                        MenuItem("Option C", () => setSelected("Option C")))),
                    Text($"Selected: {selected}").Foreground(Theme.SecondaryText)),
                sourceCode: @"
DropDownButton(""Choose"", MenuItems(
    MenuItem(""Option A"", () => setSelected(""Option A"")),
    MenuItem(""Option B"", () => setSelected(""Option B"")),
    MenuItem(""Option C"", () => setSelected(""Option C""))))
")
        ).Margin(36, 24, 36, 36));
    }
}
