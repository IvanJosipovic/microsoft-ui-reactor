using Duct;
using Duct.Core;
using Duct.Flex;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct;

class ItemsViewPage : Component
{
    record Product(string Name, string Category, double Price);

    public override Element Render()
    {
        var products = new Product[]
        {
            new("Laptop", "Electronics", 999.99),
            new("Headphones", "Electronics", 149.99),
            new("Notebook", "Office", 4.99),
            new("Pen Set", "Office", 12.99),
            new("Backpack", "Travel", 59.99),
            new("Water Bottle", "Travel", 19.99),
        }.ToList().AsReadOnly();

        return ScrollView(
            VStack(16,
                PageHeader("ItemsView", "A flexible, data-driven control for displaying collections."),

                SampleCard("Basic ItemsView",
                    ItemsView(
                        products,
                        p => p.Name,
                        (p, i) => Border(
                            VStack(4,
                                Text(p.Name).Bold(),
                                Text(p.Category).ApplyStyle("CaptionTextBlockStyle").Foreground(Theme.SecondaryText),
                                Text($"${p.Price:F2}").Foreground(Theme.SystemSuccess)
                            ).Padding(12)
                        ).Background(Theme.SubtleFill).CornerRadius(ThemeResource.CornerRadius("ControlCornerRadius").TopLeft).Margin(4)
                    ).Height(300),
                    @"ItemsView(\n    products,\n    p => p.Name,\n    (p, i) => Border(VStack(\n        Text(p.Name).Bold(),\n        Text($""${p.Price:F2}"")\n    ))\n)"),

                SampleCard("Compact ItemsView",
                    ItemsView(
                        products,
                        p => p.Name,
                        (p, i) => HStack(8,
                            Text($"{i + 1}.").Width(20).Foreground(Theme.SecondaryText),
                            Text(p.Name).Flex(grow: 1),
                            Text($"${p.Price:F2}").Foreground(Theme.AccentText)
                        ).Padding(8)
                    ).Height(250),
                    @"ItemsView(\n    products, p => p.Name,\n    (p, i) => HStack(8, Text(p.Name).Flex(grow:1), Text(price))\n)")
            ).Margin(36, 24, 36, 36)
        );
    }
}
