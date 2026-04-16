using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct.ControlPages.StatusAndInfo;

class ProgressBarPage : Component
{
    public override Element Render()
    {
        var (value, setValue) = UseState(45.0);
        var (showIndeterminate, setShowIndeterminate) = UseState(true);

        return ScrollView(
            VStack(16,
                PageHeader("ProgressBar",
                    "A horizontal bar that shows progress of an operation."),

                SampleCard("Determinate ProgressBar",
                    VStack(8,
                        Progress(value).Width(300),
                        Text($"Progress: {value:F0}%").Foreground(Theme.SecondaryText),
                        Slider(value, 0, 100, v => setValue(v)).Width(300)
                    ),
                    @"Progress(value).Width(300)",
                    options: OptionPanel(
                        Slider(value, 0, 100, v => setValue(v))
                    )),

                SampleCard("Indeterminate ProgressBar",
                    VStack(8,
                        showIndeterminate
                            ? ProgressIndeterminate().Width(300)
                            : Progress(100).Width(300),
                        ToggleSwitch(showIndeterminate, b => setShowIndeterminate(b),
                            onContent: "Loading", offContent: "Complete")
                    ),
                    @"ProgressIndeterminate().Width(300)")
            ).Margin(36, 24, 36, 36)
        );
    }
}
