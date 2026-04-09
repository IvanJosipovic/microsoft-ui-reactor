using Duct;
using Duct.Core;
using static Duct.UI;
using Microsoft.UI.Xaml;

DuctApp.Run<CollectionsApp>("Collections", width: 700, height: 600
#if DEBUG
    , preview: true
#endif
);

// <snippet:sample-data>
record Contact(string Id, string Name, string Email);

static class SampleData
{
    public static readonly List<Contact> Contacts =
        Enumerable.Range(1, 50).Select(i =>
            new Contact($"c{i}", $"Contact {i}",
                $"user{i}@example.com")
        ).ToList();
}
// </snippet:sample-data>

// <snippet:listview>
class ListViewDemo : Component
{
    public override Element Render()
    {
        var contacts = SampleData.Contacts.Take(10).ToList();

        return VStack(12,
            SubHeading("ListView"),
            ListView<Contact>(
                contacts,
                c => c.Id,
                (contact, index) =>
                    HStack(12,
                        Text(contact.Name).Bold(),
                        Text(contact.Email).Opacity(0.6)
                    ).Padding(8)
            ).Height(300)
        ).Padding(24);
    }
}
// </snippet:listview>

// <snippet:lazyvstack>
class LazyVStackDemo : Component
{
    public override Element Render()
    {
        var contacts = SampleData.Contacts;

        return VStack(12,
            SubHeading($"LazyVStack ({contacts.Count} items)"),
            LazyVStack<Contact>(
                contacts,
                c => c.Id,
                (contact, index) =>
                    HStack(12,
                        Text($"{index + 1}.").Width(30),
                        Text(contact.Name).Bold(),
                        Text(contact.Email).Opacity(0.6)
                    ).Padding(8)
            ).Height(300)
        ).Padding(24);
    }
}
// </snippet:lazyvstack>

// <snippet:gridview>
class GridViewDemo : Component
{
    public override Element Render()
    {
        var contacts = SampleData.Contacts.Take(12).ToList();

        return VStack(12,
            SubHeading("GridView"),
            GridView<Contact>(
                contacts,
                c => c.Id,
                (contact, index) =>
                    VStack(4,
                        Text(contact.Name).Bold(),
                        Text(contact.Email).FontSize(12).Opacity(0.6)
                    ).Padding(12)
                     .Background("#f5f5f5")
                     .CornerRadius(8)
                     .Width(160).Height(80)
            ).Height(300)
        ).Padding(24);
    }
}
// </snippet:gridview>

// <snippet:foreach>
class ForEachDemo : Component
{
    public override Element Render()
    {
        var colors = new[]
        {
            ("Red", "#ff4444"), ("Green", "#44ff44"),
            ("Blue", "#4444ff"), ("Yellow", "#ffff44")
        };

        return VStack(12,
            SubHeading("ForEach (non-virtualized)"),
            HStack(8,
                ForEach(colors, ((string Name, string Hex) color) =>
                    Text(color.Name)
                        .Padding(8, 16)
                        .Background(color.Hex)
                        .CornerRadius(4)
                        .WithKey(color.Name)
                )
            )
        ).Padding(24);
    }
}
// </snippet:foreach>

// <snippet:withkey>
class WithKeyDemo : Component
{
    public override Element Render()
    {
        var (items, updateItems) = UseReducer(
            new List<string> { "Apple", "Banana", "Cherry" });
        var (newItem, setNewItem) = UseState("");

        return VStack(12,
            SubHeading("Stable Identity with WithKey"),
            HStack(8,
                TextField(newItem, setNewItem, placeholder: "New item"),
                Button("Add", () => {
                    if (!string.IsNullOrWhiteSpace(newItem)) {
                        updateItems(l => [.. l, newItem.Trim()]);
                        setNewItem("");
                    }
                })
            ),
            VStack(4, items.Select((item, i) =>
                HStack(8,
                    Text(item),
                    Button("Remove", () => updateItems(
                        l => l.Where((_, idx) => idx != i).ToList()))
                ).WithKey($"item-{item}-{i}")
            ).ToArray())
        ).Padding(24);
    }
}
// </snippet:withkey>

class CollectionsApp : Component
{
    public override Element Render()
    {
        return ScrollView(
            VStack(24,
                Heading("Collections"),
                Component<ListViewDemo>(),
                Component<LazyVStackDemo>(),
                Component<GridViewDemo>(),
                Component<ForEachDemo>(),
                Component<WithKeyDemo>()
            ).Padding(24)
        );
    }
}
