using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct.ControlPages.StatusAndInfo;

class InfoBadgePage : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(5);

        return ScrollView(
            VStack(16,
                PageHeader("InfoBadge",
                    "A small indicator that conveys status on another element."),

                SampleCard("Numeric Badge",
                    VStack(8,
                        HStack(16,
                            VStack(4,
                                Text("Notifications").Foreground(Theme.PrimaryText),
                                InfoBadge(count)
                            ),
                            VStack(4,
                                Text("Messages").Foreground(Theme.PrimaryText),
                                InfoBadge(42)
                            )
                        ),
                        HStack(8,
                            Button("Increment", () => setCount(count + 1)),
                            Button("Reset", () => setCount(0))
                        )
                    ),
                    @"InfoBadge(count)  // numeric badge
InfoBadge(42)"),

                SampleCard("Dot Badge",
                    HStack(16,
                        VStack(4,
                            Text("Status").Foreground(Theme.PrimaryText),
                            InfoBadge()
                        ),
                        VStack(4,
                            Text("Updates Available").Foreground(Theme.PrimaryText),
                            InfoBadge()
                        )
                    ),
                    @"InfoBadge()  // dot indicator")
            ).Margin(36, 24, 36, 36)
        );
    }
}
