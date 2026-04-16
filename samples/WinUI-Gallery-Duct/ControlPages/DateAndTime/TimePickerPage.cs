using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct.ControlPages.DateAndTime;

class TimePickerPage : Component
{
    public override Element Render()
    {
        var (time, setTime) = UseState(DateTime.Now.TimeOfDay);

        return ScrollView(
            VStack(16,
                PageHeader("TimePicker",
                    "A control that lets a user pick a time using spinners."),

                SampleCard("Basic TimePicker",
                    VStack(8,
                        TimePicker(time, t => setTime(t)),
                        Text($"Selected: {DateTime.Today.Add(time):t}")
                            .Foreground(Theme.SecondaryText)
                    ),
                    @"TimePicker(time, t => setTime(t))"),

                SampleCard("TimePicker with Preset",
                    VStack(8,
                        TimePicker(time, t => setTime(t)),
                        HStack(8,
                            Button("Set to Noon", () => setTime(new TimeSpan(12, 0, 0))),
                            Button("Set to Now", () => setTime(DateTime.Now.TimeOfDay))
                        )
                    ),
                    @"TimePicker(time, t => setTime(t))
Button(""Noon"", () => setTime(new TimeSpan(12, 0, 0)))")
            ).Margin(36, 24, 36, 36)
        );
    }
}
