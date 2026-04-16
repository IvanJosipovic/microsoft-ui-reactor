using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct.ControlPages.BasicInput;

class CheckBoxPage: Component
{
    public override Element Render()
    {
        var (isChecked, setIsChecked) = UseState(false);
        var (threeState, setThreeState) = UseState<bool?>(null);

        return ScrollView(VStack(16,
            PageHeader("CheckBox", "A control that a user can select or clear."),

            SampleCard("Two-State CheckBox",
                VStack(8,
                    CheckBox(isChecked, v => setIsChecked(v), "I agree to the terms"),
                    Text($"Checked: {isChecked}").Foreground(Theme.SecondaryText)),
                sourceCode: @"
CheckBox(isChecked, v => setIsChecked(v), ""I agree to the terms"")
"),

            SampleCard("Three-State CheckBox",
                VStack(8,
                    ThreeStateCheckBox(threeState, v => setThreeState(v), "Select all"),
                    Text($"State: {(threeState == null ? "Indeterminate" : threeState.ToString())}").Foreground(Theme.SecondaryText)),
                sourceCode: @"
ThreeStateCheckBox(threeState, v => setThreeState(v), ""Select all"")
")
        ).Margin(36, 24, 36, 36));
    }
}
