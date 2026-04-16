using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct.ControlPages.DateAndTime;

class CalendarViewPage : Component
{
    public override Element Render()
    {
        return ScrollView(
            VStack(16,
                PageHeader("CalendarView",
                    "A calendar display that lets a user select a date."),

                SampleCard("Basic CalendarView",
                    CalendarView().Width(300).Height(350),
                    @"CalendarView()"),

                SampleCard("CalendarView in Card",
                    Border(
                        CalendarView().Width(300).Height(350)
                    ).Background(Theme.CardBackground)
                     .WithBorder(Theme.CardStroke)
                     .CornerRadius(8)
                     .Padding(8),
                    @"Border(
    CalendarView().Width(300).Height(350)
).Background(Theme.CardBackground)
 .WithBorder(Theme.CardStroke)
 .CornerRadius(8)
 .Padding(8)")
            ).Margin(36, 24, 36, 36)
        );
    }
}
