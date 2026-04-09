using Duct;
using Duct.Core;
using static Duct.UI;
using Microsoft.UI.Xaml;

DuctApp.Run<StylingApp>("Styling and Theming", width: 650, height: 700
#if DEBUG
    , preview: true
#endif
);

// <snippet:theme-tokens>
class ThemeTokensExample : Component
{
    public override Element Render()
    {
        return VStack(12,
            Text("Primary Text").Foreground(Theme.PrimaryText),
            Text("Secondary Text").Foreground(Theme.SecondaryText),
            Text("Accent Text").Foreground(Theme.AccentText).SemiBold(),
            Text("On Accent Background")
                .Foreground("#FFFFFF")
                .Padding(8, 4)
                .Background(Theme.Accent)
                .CornerRadius(4)
        ).Padding(24);
    }
}
// </snippet:theme-tokens>

// <snippet:card-layout>
class CardLayoutExample : Component
{
    public override Element Render()
    {
        return VStack(16,
            Heading("Dashboard"),
            HStack(12,
                Card("Users", "1,204", Theme.Accent),
                Card("Revenue", "$48.2k", Theme.SystemSuccess),
                Card("Errors", "3", Theme.SystemCritical)
            )
        ).Padding(24);
    }

    static Element Card(string title, string value, ThemeRef accent) =>
        Border(
            VStack(8,
                Caption(title).Foreground(Theme.SecondaryText),
                Text(value).FontSize(28).Bold().Foreground(accent)
            ).Padding(16)
        ).Background(Theme.CardBackground)
         .CornerRadius(8)
         .WithBorder(Theme.CardStroke, 1)
         .Width(160);
}
// </snippet:card-layout>

// <snippet:color-modifiers>
class ColorModifiersExample : Component
{
    public override Element Render()
    {
        return VStack(8,
            Text("Theme token").Background(Theme.SubtleFill).Padding(8),
            Text("Hex string").Background("#E8F5E9").Padding(8),
            Text("Mixed").Foreground(Theme.PrimaryText)
                .Background("#1E1E2E").Padding(8)
        ).Padding(24);
    }
}
// </snippet:color-modifiers>

// <snippet:signal-colors>
class SignalColorsExample : Component
{
    public override Element Render()
    {
        return HStack(12,
            Badge("Info", Theme.SystemAttention),
            Badge("Success", Theme.SystemSuccess),
            Badge("Warning", Theme.SystemCaution),
            Badge("Error", Theme.SystemCritical)
        ).Padding(24);
    }

    static Element Badge(string label, ThemeRef color) =>
        Text(label)
            .FontSize(12).SemiBold()
            .Foreground(color)
            .Padding(8, 4)
            .Background(Theme.SubtleFill)
            .CornerRadius(4);
}
// </snippet:signal-colors>

// <snippet:dark-light-toggle>
class DarkLightToggleExample : Component
{
    public override Element Render()
    {
        var (isDark, setIsDark) = UseState(false);

        return VStack(16,
            ToggleSwitch(isDark, setIsDark, onContent: "Dark", offContent: "Light"),
            Border(
                VStack(12,
                    Text("This panel follows the toggle.").Foreground(Theme.PrimaryText),
                    Text("Background adapts automatically.").Foreground(Theme.SecondaryText)
                ).Padding(16)
            ).Background(Theme.CardBackground)
             .CornerRadius(8)
             .Set(b => b.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light)
        ).Padding(24);
    }
}
// </snippet:dark-light-toggle>

// <snippet:custom-resource>
class CustomResourceExample : Component
{
    public override Element Render()
    {
        return VStack(12,
            Text("Using a named WinUI resource:")
                .Foreground(Theme.PrimaryText),
            Text("NavigationViewItemForeground")
                .Foreground(Theme.Ref("NavigationViewItemForeground"))
        ).Padding(24);
    }
}
// </snippet:custom-resource>

// Main app showing all examples
class StylingApp : Component
{
    public override Element Render()
    {
        return ScrollView(
            VStack(24,
                Heading("Styling and Theming"),
                ThemeTokensExample(),
                CardLayoutExample(),
                SignalColorsExample(),
                DarkLightToggleExample()
            ).Padding(24)
        );
    }

    static Element ThemeTokensExample() => Component<ThemeTokensExample>();
    static Element CardLayoutExample() => Component<CardLayoutExample>();
    static Element SignalColorsExample() => Component<SignalColorsExample>();
    static Element DarkLightToggleExample() => Component<DarkLightToggleExample>();
}
