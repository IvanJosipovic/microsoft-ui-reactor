using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct.ControlPages.Styles;

class TypographyPage : Component
{
    record TypeRampEntry(
        string StyleName, string ExampleText,
        int Size, string Weight, string VariableFont,
        string Recommendation);

    static readonly TypeRampEntry[] TypeRamp =
    [
        new("DisplayTextBlockStyle",    "Display",       68, "SemiBold", "wght 600",
            "Hero banners and splash screens. Use sparingly — one per page at most."),
        new("TitleLargeTextBlockStyle", "Title Large",   40, "SemiBold", "wght 600",
            "Primary page titles on feature or landing pages."),
        new("TitleTextBlockStyle",      "Title",         28, "SemiBold", "wght 600",
            "Page titles, dialog headings, and top-level section titles."),
        new("SubtitleTextBlockStyle",   "Subtitle",      20, "SemiBold", "wght 600",
            "Section headings within a page, card group labels."),
        new("BodyStrongTextBlockStyle", "Body Strong",   14, "SemiBold", "wght 600",
            "Emphasized body text, inline labels, and field headers."),
        new("BodyTextBlockStyle",       "Body",          14, "Normal",   "wght 400",
            "Default body text for paragraphs, descriptions, and general content."),
        new("CaptionTextBlockStyle",    "Caption",       12, "Normal",   "wght 400",
            "Secondary labels, timestamps, footnotes, and helper text."),
    ];

    public override Element Render()
    {
        return PageContent("Typography",
            "WinUI's type system uses Segoe UI Variable to create a clear visual hierarchy. Each level in the type ramp has a defined size, weight, and line height. Use the built-in text block styles to keep your UI consistent with the system design language.",

            TypeRampSection(),
            TextHierarchySection(),
            FontSizesSection(),
            FontWeightsSection(),
            ApplyStyleSection(),
            TextWrappingSection(),
            TextColorSection()
        );
    }

    // ── Section 1: Type ramp reference ───────────────────────────────

    static Element TypeRampSection()
    {
        return SampleCard("Type Ramp",
            VStack(8,
                Text("WinUI defines a set of TextBlock styles that map to Segoe UI Variable at specific sizes and weights. Use these styles rather than setting font size and weight manually to stay consistent with the system type ramp.")
                    .Foreground(Theme.SecondaryText)
                    .FontSize(13)
                    .Set(tb => tb.TextWrapping = TextWrapping.Wrap)
                    .Margin(0, 0, 0, 8),

                VStack(0, TypeRamp.Select(TypeRampRow).ToArray())
            ),
            string.Join("\n", TypeRamp.Select(e =>
                $"Text(\"{e.ExampleText}\").ApplyStyle(\"{e.StyleName}\")  // {e.Size}px {e.Weight}")));
    }

    static Element TypeRampRow(TypeRampEntry entry) =>
        Border(
            Grid(
                columns: ["*", "220", "200"], rows: ["Auto"],

                // Live preview
                Text(entry.ExampleText)
                    .ApplyStyle(entry.StyleName)
                    .Foreground(Theme.PrimaryText)
                    .VAlign(VerticalAlignment.Center)
                    .Grid(column: 0),

                // Metadata
                VStack(0,
                    Text(entry.StyleName)
                        .FontSize(11)
                        .Foreground(Theme.TertiaryText)
                        .Set(tb => tb.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Cascadia Code, Consolas, monospace")),
                    Text($"{entry.Size}px · {entry.Weight} · {entry.VariableFont}")
                        .FontSize(11)
                        .Foreground(Theme.SecondaryText)
                ).VAlign(VerticalAlignment.Center).Grid(column: 1),

                // Recommendation
                Text(entry.Recommendation)
                    .FontSize(12)
                    .Foreground(Theme.SecondaryText)
                    .Set(tb => tb.TextWrapping = TextWrapping.Wrap)
                    .VAlign(VerticalAlignment.Center)
                    .Grid(column: 2)
            ).Margin(0, 8, 0, 8)
        )
        .WithBorder(Theme.DividerStroke)
        .Set(b =>
        {
            var border = (Microsoft.UI.Xaml.Controls.Border)b;
            border.BorderThickness = new Thickness(0, 0, 0, 1);
        });

    // ── Section 2: Text hierarchy (kept from original) ───────────────

    static Element TextHierarchySection() =>
        SampleCard("Text Hierarchy Helpers",
            VStack(12,
                Text("Duct provides shorthand helpers that map to common type ramp levels without requiring ApplyStyle. These set font size and weight directly.")
                    .Foreground(Theme.SecondaryText)
                    .FontSize(13)
                    .Set(tb => tb.TextWrapping = TextWrapping.Wrap)
                    .Margin(0, 0, 0, 4),

                VStack(8,
                    HStack(12,
                        Heading("Heading").Foreground(Theme.PrimaryText),
                        Text("— 28px Bold").FontSize(12).Foreground(Theme.TertiaryText).VAlign(VerticalAlignment.Bottom)
                    ),
                    HStack(12,
                        SubHeading("SubHeading").Foreground(Theme.PrimaryText),
                        Text("— 20px SemiBold").FontSize(12).Foreground(Theme.TertiaryText).VAlign(VerticalAlignment.Bottom)
                    ),
                    HStack(12,
                        Text("Body Text").Foreground(Theme.PrimaryText),
                        Text("— 14px Normal (default)").FontSize(12).Foreground(Theme.TertiaryText).VAlign(VerticalAlignment.Bottom)
                    ),
                    HStack(12,
                        Caption("Caption").Foreground(Theme.SecondaryText),
                        Text("— 12px Normal").FontSize(12).Foreground(Theme.TertiaryText).VAlign(VerticalAlignment.Bottom)
                    )
                )
            ),
            @"Heading(""Heading"")       // 28px Bold
SubHeading(""SubHeading"") // 20px SemiBold
Text(""Body Text"")        // 14px Normal
Caption(""Caption"")       // 12px Normal");

    // ── Section 3: Font sizes (kept from original) ───────────────────

    static Element FontSizesSection() =>
        SampleCard("Font Sizes",
            VStack(4,
                Text("10px").FontSize(10).Foreground(Theme.PrimaryText),
                Text("12px").FontSize(12).Foreground(Theme.PrimaryText),
                Text("14px (default)").FontSize(14).Foreground(Theme.PrimaryText),
                Text("16px").FontSize(16).Foreground(Theme.PrimaryText),
                Text("20px").FontSize(20).Foreground(Theme.PrimaryText),
                Text("24px").FontSize(24).Foreground(Theme.PrimaryText),
                Text("32px").FontSize(32).Foreground(Theme.PrimaryText)
            ),
            @"Text(""14px"").FontSize(14)
Text(""20px"").FontSize(20)
Text(""32px"").FontSize(32)");

    // ── Section 4: Font weights (kept from original) ─────────────────

    static Element FontWeightsSection() =>
        SampleCard("Font Weights",
            VStack(4,
                Text("Normal weight").Foreground(Theme.PrimaryText),
                Text("SemiBold weight").Foreground(Theme.PrimaryText).SemiBold(),
                Text("Bold weight").Foreground(Theme.PrimaryText).Bold()
            ),
            @"Text(""Normal weight"")
Text(""SemiBold"").SemiBold()
Text(""Bold"").Bold()");

    // ── Section 5: ApplyStyle usage ──────────────────────────────────

    static Element ApplyStyleSection() =>
        SampleCard("Applying XAML Text Styles",
            VStack(12,
                Text("Use .ApplyStyle() with any WinUI TextBlock style key to apply the full type ramp settings (size, weight, line height, and optical sizing) in one call.")
                    .Foreground(Theme.SecondaryText)
                    .FontSize(13)
                    .Set(tb => tb.TextWrapping = TextWrapping.Wrap)
                    .Margin(0, 0, 0, 4),

                VStack(8,
                    Text("Title style").ApplyStyle("TitleTextBlockStyle").Foreground(Theme.PrimaryText),
                    Text("Subtitle style").ApplyStyle("SubtitleTextBlockStyle").Foreground(Theme.PrimaryText),
                    Text("Body Strong style").ApplyStyle("BodyStrongTextBlockStyle").Foreground(Theme.PrimaryText),
                    Text("Body style").ApplyStyle("BodyTextBlockStyle").Foreground(Theme.PrimaryText),
                    Text("Caption style").ApplyStyle("CaptionTextBlockStyle").Foreground(Theme.SecondaryText)
                )
            ),
            @"Text(""Title"").ApplyStyle(""TitleTextBlockStyle"")
Text(""Subtitle"").ApplyStyle(""SubtitleTextBlockStyle"")
Text(""Body Strong"").ApplyStyle(""BodyStrongTextBlockStyle"")
Text(""Body"").ApplyStyle(""BodyTextBlockStyle"")
Text(""Caption"").ApplyStyle(""CaptionTextBlockStyle"")");

    // ── Section 6: Text wrapping & trimming ──────────────────────────

    static Element TextWrappingSection() =>
        SampleCard("Text Wrapping & Trimming",
            VStack(16,
                Text("Control how text behaves when it exceeds the available width. Wrapping moves overflow to the next line; trimming truncates with an ellipsis.")
                    .Foreground(Theme.SecondaryText)
                    .FontSize(13)
                    .Set(tb => tb.TextWrapping = TextWrapping.Wrap)
                    .Margin(0, 0, 0, 4),

                Text("Wrapping").SemiBold().Foreground(Theme.PrimaryText),
                Border(
                    Text("This is a long paragraph of text that wraps to the next line when it exceeds the available width of its container, creating a natural reading flow.")
                        .Foreground(Theme.PrimaryText)
                        .Set(tb => tb.TextWrapping = TextWrapping.Wrap)
                )
                .Background(Theme.SubtleFill)
                .WithBorder(Theme.DividerStroke)
                .CornerRadius(6)
                .Padding(12)
                .Width(280),

                Text("Trimming").SemiBold().Foreground(Theme.PrimaryText),
                Border(
                    Text("This is a long line of text that will be trimmed with an ellipsis when it reaches the edge of its container.")
                        .Foreground(Theme.PrimaryText)
                        .Set(tb =>
                        {
                            tb.TextWrapping = TextWrapping.NoWrap;
                            tb.TextTrimming = TextTrimming.CharacterEllipsis;
                        })
                )
                .Background(Theme.SubtleFill)
                .WithBorder(Theme.DividerStroke)
                .CornerRadius(6)
                .Padding(12)
                .Width(280),

                Text("Max Lines").SemiBold().Foreground(Theme.PrimaryText),
                Border(
                    Text("This paragraph wraps but is limited to two lines. Any additional content beyond the second line is trimmed with an ellipsis to keep the layout compact and predictable.")
                        .Foreground(Theme.PrimaryText)
                        .Set(tb =>
                        {
                            tb.TextWrapping = TextWrapping.Wrap;
                            tb.TextTrimming = TextTrimming.CharacterEllipsis;
                            tb.MaxLines = 2;
                        })
                )
                .Background(Theme.SubtleFill)
                .WithBorder(Theme.DividerStroke)
                .CornerRadius(6)
                .Padding(12)
                .Width(280)
            ),
            @"// Wrapping
Text(""Long text..."")
    .Set(tb => tb.TextWrapping = TextWrapping.Wrap)

// Trimming with ellipsis
Text(""Long text..."")
    .Set(tb => {
        tb.TextWrapping = TextWrapping.NoWrap;
        tb.TextTrimming = TextTrimming.CharacterEllipsis;
    })

// Max lines with trimming
Text(""Long text..."")
    .Set(tb => {
        tb.TextWrapping = TextWrapping.Wrap;
        tb.TextTrimming = TextTrimming.CharacterEllipsis;
        tb.MaxLines = 2;
    })");

    // ── Section 7: Text color with typography ────────────────────────

    static Element TextColorSection() =>
        SampleCard("Text Color & Hierarchy",
            VStack(12,
                Text("Combine type ramp levels with text color tokens to build clear visual hierarchy. Use primary text for main content, secondary for supporting info, and accent for interactive or highlighted text.")
                    .Foreground(Theme.SecondaryText)
                    .FontSize(13)
                    .Set(tb => tb.TextWrapping = TextWrapping.Wrap)
                    .Margin(0, 0, 0, 4),

                // Practical hierarchy example
                Border(
                    VStack(8,
                        Text("Notifications").ApplyStyle("SubtitleTextBlockStyle").Foreground(Theme.PrimaryText),
                        Border(VStack()).Height(1).Background(Theme.DividerStroke).HAlign(HorizontalAlignment.Stretch),
                        VStack(4,
                            Text("New update available").SemiBold().Foreground(Theme.PrimaryText),
                            Text("Version 2.5 includes performance improvements and bug fixes.")
                                .FontSize(13).Foreground(Theme.SecondaryText)
                                .Set(tb => tb.TextWrapping = TextWrapping.Wrap),
                            Text("Install now").FontSize(13).Foreground(Theme.AccentText)
                        ),
                        Border(VStack()).Height(1).Background(Theme.DividerStroke).HAlign(HorizontalAlignment.Stretch),
                        VStack(4,
                            Text("Scheduled maintenance").SemiBold().Foreground(Theme.PrimaryText),
                            Text("The system will be briefly unavailable on Sunday at 2:00 AM.")
                                .FontSize(13).Foreground(Theme.SecondaryText)
                                .Set(tb => tb.TextWrapping = TextWrapping.Wrap),
                            Text("3 hours ago").FontSize(12).Foreground(Theme.TertiaryText)
                        )
                    ).Margin(16)
                )
                .Background(Theme.CardBackground)
                .WithBorder(Theme.CardStroke)
                .CornerRadius(8)
                .Width(360)
            ),
            @"// Heading — Subtitle style, primary color
Text(""Notifications"")
    .ApplyStyle(""SubtitleTextBlockStyle"")
    .Foreground(Theme.PrimaryText)

// Title — SemiBold, primary color
Text(""New update"").SemiBold()
    .Foreground(Theme.PrimaryText)

// Description — smaller, secondary color
Text(""Details..."").FontSize(13)
    .Foreground(Theme.SecondaryText)

// Action link — accent color
Text(""Install now"").FontSize(13)
    .Foreground(Theme.AccentText)

// Timestamp — smallest, tertiary color
Text(""3 hours ago"").FontSize(12)
    .Foreground(Theme.TertiaryText)");
}
