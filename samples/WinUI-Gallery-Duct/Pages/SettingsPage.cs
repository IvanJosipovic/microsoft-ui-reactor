using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using static Duct.UI;

namespace WinUIGalleryDuct;

class SettingsPage : Component
{
    public override Element Render()
    {
        return ScrollView(
            VStack(24,
                // Page header
                Text("Settings")
                    .FontSize(28)
                    .Bold()
                    .Foreground(Theme.PrimaryText),

                // About section card
                Border(
                    VStack(12,
                        Text("About this app")
                            .Foreground(Theme.PrimaryText)
                            .SemiBold(),

                        Border(VStack())
                            .Height(1)
                            .Background(Theme.DividerStroke),

                        HStack(16,
                            // App icon placeholder
                            Border(
                                Text("\uE80F")
                                    .Set(tb => tb.FontFamily = new FontFamily("Segoe MDL2 Assets"))
                                    .FontSize(24)
                                    .Foreground(Theme.AccentText)
                                    .Center()
                            )
                            .Background(Theme.SubtleFill)
                            .CornerRadius(8)
                            .Width(48).Height(48),

                            VStack(2,
                                Text("WinUI Gallery (Duct)")
                                    .Foreground(Theme.PrimaryText)
                                    .SemiBold(),
                                Text("Version 1.0")
                                    .Foreground(Theme.SecondaryText)
                                    .FontSize(12)
                            ).VAlign(VerticalAlignment.Center)
                        ),

                        Text("This app is built with Duct, a declarative component-based UI framework for WinUI 3. It demonstrates how to recreate the WinUI Gallery experience using reactive hooks and a composable element DSL.")
                            .Foreground(Theme.SecondaryText)
                            .FontSize(13)
                            .Set(tb => tb.TextWrapping = TextWrapping.Wrap)
                    )
                )
                .Padding(20)
                .Background(Theme.CardBackground)
                .WithBorder(Theme.CardStroke)
                .CornerRadius(8)
                .MaxWidth(600),

                // Links section card
                Border(
                    VStack(12,
                        Text("Links")
                            .Foreground(Theme.PrimaryText)
                            .SemiBold(),

                        Border(VStack())
                            .Height(1)
                            .Background(Theme.DividerStroke),

                        HyperlinkButton("Source code on GitHub",
                            new Uri("https://github.com/AhmedWaleed/WinUI-Gallery")),

                        HyperlinkButton("WinUI Gallery (original)",
                            new Uri("https://github.com/microsoft/WinUI-Gallery")),

                        HyperlinkButton("Fluent Design guidelines",
                            new Uri("https://learn.microsoft.com/en-us/windows/apps/design/"))
                    )
                )
                .Padding(20)
                .Background(Theme.CardBackground)
                .WithBorder(Theme.CardStroke)
                .CornerRadius(8)
                .MaxWidth(600),

                // Framework info card
                Border(
                    VStack(8,
                        Text("Built with Duct")
                            .Foreground(Theme.PrimaryText)
                            .SemiBold(),

                        Border(VStack())
                            .Height(1)
                            .Background(Theme.DividerStroke),

                        HStack(8,
                            Text("Framework").Foreground(Theme.SecondaryText).FontSize(13).Width(120),
                            Text("Duct (declarative C# DSL)").Foreground(Theme.PrimaryText).FontSize(13)
                        ),
                        HStack(8,
                            Text("Platform").Foreground(Theme.SecondaryText).FontSize(13).Width(120),
                            Text("WinUI 3 / Windows App SDK").Foreground(Theme.PrimaryText).FontSize(13)
                        ),
                        HStack(8,
                            Text("Rendering").Foreground(Theme.SecondaryText).FontSize(13).Width(120),
                            Text("Virtual DOM reconciler").Foreground(Theme.PrimaryText).FontSize(13)
                        ),
                        HStack(8,
                            Text("State").Foreground(Theme.SecondaryText).FontSize(13).Width(120),
                            Text("React-style hooks").Foreground(Theme.PrimaryText).FontSize(13)
                        )
                    )
                )
                .Padding(20)
                .Background(Theme.CardBackground)
                .WithBorder(Theme.CardStroke)
                .CornerRadius(8)
                .MaxWidth(600)

            ).Margin(36, 24, 36, 48)
        );
    }
}
