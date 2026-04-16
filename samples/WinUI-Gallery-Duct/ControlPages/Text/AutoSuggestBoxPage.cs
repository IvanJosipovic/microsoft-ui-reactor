using Duct;
using Duct.Core;
using Duct.Flex;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct;

class AutoSuggestBoxPage : Component
{
    public override Element Render()
    {
        var (query, setQuery) = UseState("");
        var (submitted, setSubmitted) = UseState("");
        var allItems = new[] { "Apple", "Apricot", "Banana", "Blueberry", "Cherry", "Coconut", "Date", "Fig", "Grape" };

        return ScrollView(
            VStack(16,
                PageHeader("AutoSuggestBox", "A text input that shows filtered suggestions as the user types."),

                SampleCard("Basic AutoSuggestBox",
                    VStack(8,
                        AutoSuggestBox(query, setQuery).Width(300),
                        Text($"Current text: \"{query}\"").Foreground(Theme.SecondaryText)
                    ),
                    @"var (query, setQuery) = UseState("""");\nAutoSuggestBox(query, setQuery)"),

                SampleCard("With Query Submitted",
                    VStack(8,
                        AutoSuggestBox(query, setQuery, s => setSubmitted(s)).Width(300),
                        When(submitted != "",
                            () => Text($"Submitted: \"{submitted}\"").Foreground(Theme.SystemSuccess))
                    ),
                    @"AutoSuggestBox(query, setQuery, s => setSubmitted(s))"),

                SampleCard("Filtered Results Display",
                    VStack(8,
                        AutoSuggestBox(query, setQuery).Width(300),
                        Text("Matching items:").Bold(),
                        VStack(2,
                            allItems
                                .Where(i => string.IsNullOrEmpty(query) || i.Contains(query, System.StringComparison.OrdinalIgnoreCase))
                                .Select(i => Text($"  • {i}").Foreground(Theme.SecondaryText))
                                .ToArray()
                        )
                    ),
                    @"AutoSuggestBox(query, setQuery)\nallItems.Where(i => i.Contains(query)).Select(...)")
            ).Margin(36, 24, 36, 36)
        );
    }
}
