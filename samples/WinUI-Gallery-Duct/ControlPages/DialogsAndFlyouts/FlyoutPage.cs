using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct.ControlPages.DialogsAndFlyouts;

class FlyoutPage : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(0);

        return ScrollView(
            VStack(16,
                PageHeader("Flyout",
                    "A lightweight popup that shows contextual information."),

                SampleCard("Basic Flyout",
                    Flyout(
                        Button("Show Flyout"),
                        Text("This is flyout content!").Padding(8)),
                    @"Flyout(
    Button(""Show Flyout""),
    Text(""This is flyout content!"").Padding(8))"),

                SampleCard("Flyout with Actions",
                    VStack(8,
                        Flyout(
                            Button("Cart Options"),
                            VStack(8,
                                Text("Your cart has items.").Foreground(Theme.PrimaryText),
                                Button("Empty Cart", () => setCount(0))
                            ).Padding(8)),
                        Text($"Cart count: {count}").Foreground(Theme.SecondaryText),
                        Button("Add Item", () => setCount(count + 1))
                    ),
                    @"Flyout(
    Button(""Cart Options""),
    VStack(8,
        Text(""Your cart has items.""),
        Button(""Empty Cart"", () => setCount(0)))
    .Padding(8))")
            ).Margin(36, 24, 36, 36)
        );
    }
}
