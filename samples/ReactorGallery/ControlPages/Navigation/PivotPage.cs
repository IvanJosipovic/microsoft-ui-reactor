using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;
using static WinUIGalleryReactor.SamplePageHost;

namespace WinUIGalleryReactor.ControlPages.Navigation;

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
                        PivotItem("All", Factories.Text("All items displayed here.").Padding(12)),
                        PivotItem("Recent", Factories.Text("Recent items displayed here.").Padding(12)),
                        PivotItem("Favorites", Factories.Text("Favorite items displayed here.").Padding(12))
                    ).Height(200),
                    @"Pivot(
    PivotItem(""All"", Factories.Text(""All items"")),
    PivotItem(""Recent"", Factories.Text(""Recent items"")),
    PivotItem(""Favorites"", Factories.Text(""Favorite items"")))"),

                SampleCard("Pivot with Rich Content",
                    Pivot(
                        PivotItem("Overview",
                            VStack(8,
                                SubHeading("Overview").Foreground(Theme.PrimaryText),
                                Factories.Text("Summary of key metrics.").Foreground(Theme.SecondaryText)
                            ).Padding(12)),
                        PivotItem("Details",
                            VStack(8,
                                SubHeading("Details").Foreground(Theme.PrimaryText),
                                Factories.Text("Detailed information goes here.").Foreground(Theme.SecondaryText)
                            ).Padding(12))
                    ).Height(200),
                    @"Pivot(
    PivotItem(""Overview"", VStack(8,
        SubHeading(""Overview""),
        Factories.Text(""Summary of key metrics.""))),
    PivotItem(""Details"", VStack(8,
        SubHeading(""Details""),
        Factories.Text(""Detailed info.""))))")
            ).Margin(36, 24, 36, 36)
        );
    }
}
