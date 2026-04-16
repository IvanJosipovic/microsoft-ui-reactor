using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct.ControlPages.Navigation;

class PivotPage : Component
{
    public override Element Render()
    {
        return ScrollView(
            VStack(16,
                PageHeader("Pivot",
                    "A tabbed interface for switching between content sections."),

                SampleCard("Basic Pivot",
                    Pivot(
                        PivotItem("All", Text("All items displayed here.").Padding(12)),
                        PivotItem("Recent", Text("Recent items displayed here.").Padding(12)),
                        PivotItem("Favorites", Text("Favorite items displayed here.").Padding(12))
                    ).Height(200),
                    @"Pivot(
    PivotItem(""All"", Text(""All items"")),
    PivotItem(""Recent"", Text(""Recent items"")),
    PivotItem(""Favorites"", Text(""Favorite items"")))"),

                SampleCard("Pivot with Rich Content",
                    Pivot(
                        PivotItem("Overview",
                            VStack(8,
                                SubHeading("Overview").Foreground(Theme.PrimaryText),
                                Text("Summary of key metrics.").Foreground(Theme.SecondaryText)
                            ).Padding(12)),
                        PivotItem("Details",
                            VStack(8,
                                SubHeading("Details").Foreground(Theme.PrimaryText),
                                Text("Detailed information goes here.").Foreground(Theme.SecondaryText)
                            ).Padding(12))
                    ).Height(200),
                    @"Pivot(
    PivotItem(""Overview"", VStack(8,
        SubHeading(""Overview""),
        Text(""Summary of key metrics.""))),
    PivotItem(""Details"", VStack(8,
        SubHeading(""Details""),
        Text(""Detailed info.""))))")
            ).Margin(36, 24, 36, 36)
        );
    }
}
