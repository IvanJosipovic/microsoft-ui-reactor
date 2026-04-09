using Duct;
using Duct.Core;
using static Duct.UI;
using Microsoft.UI.Xaml;

DuctApp.Run<AnimationApp>("Animation", width: 650, height: 700
#if DEBUG
    , preview: true
#endif
);

// <snippet:opacity-transition>
class OpacityDemo : Component
{
    public override Element Render()
    {
        var (visible, setVisible) = UseState(true);

        return VStack(12,
            SubHeading("Opacity Transition"),
            Button(visible ? "Fade Out" : "Fade In",
                () => setVisible(!visible)),
            Text("This text fades in and out")
                .FontSize(18).Bold()
                .Opacity(visible ? 1.0 : 0.0)
                .OpacityTransition(TimeSpan.FromMilliseconds(500))
        ).Padding(24);
    }
}
// </snippet:opacity-transition>

// <snippet:scale-transition>
class ScaleDemo : Component
{
    public override Element Render()
    {
        var (enlarged, setEnlarged) = UseState(false);

        return VStack(12,
            SubHeading("Scale Transition"),
            Button(enlarged ? "Shrink" : "Enlarge",
                () => setEnlarged(!enlarged)),
            Border(
                Text("Scales up and down").FontSize(18).Bold()
            ).Padding(12)
             .CornerRadius(8)
             .Background("#e8e8e8")
             .ScaleTransition()
        ).Padding(24);
    }
}
// </snippet:scale-transition>

// <snippet:translation-transition>
class TranslationDemo : Component
{
    public override Element Render()
    {
        var (moved, setMoved) = UseState(false);

        return VStack(12,
            SubHeading("Translation Transition"),
            Button(moved ? "Slide Back" : "Slide Right",
                () => setMoved(!moved)),
            Text("Slides horizontally")
                .FontSize(18).Bold()
                .Translation(moved ? 120f : 0f, 0f, 0f)
                .TranslationTransition()
        ).Padding(24);
    }
}
// </snippet:translation-transition>

// <snippet:background-transition>
class BackgroundDemo : Component
{
    public override Element Render()
    {
        var (warm, setWarm) = UseState(false);

        return VStack(12,
            SubHeading("Background Transition"),
            Button(warm ? "Cool Colors" : "Warm Colors",
                () => setWarm(!warm)),
            VStack(8,
                Text("Background animates between colors")
                    .Foreground("#ffffff").Bold()
            ).Padding(16)
             .CornerRadius(8)
             .Background(warm ? "#da3b01" : "#0078d4")
             .BackgroundTransition(TimeSpan.FromMilliseconds(600))
        ).Padding(24);
    }
}
// </snippet:background-transition>

// <snippet:combined-transitions>
class CombinedDemo : Component
{
    public override Element Render()
    {
        var (active, setActive) = UseState(false);

        return VStack(12,
            SubHeading("Combined Transitions"),
            Button(active ? "Reset" : "Animate",
                () => setActive(!active)),
            Border(
                Text("All at once").FontSize(16).Bold()
                    .Foreground("#ffffff")
            ).Padding(16)
             .CornerRadius(8)
             .Background("#7b2ab5")
             .Opacity(active ? 1.0 : 0.4)
             .Translation(active ? 40f : 0f, 0f, 0f)
             .OpacityTransition(TimeSpan.FromMilliseconds(400))
             .TranslationTransition()
        ).Padding(24);
    }
}
// </snippet:combined-transitions>

// <snippet:layout-animation>
class LayoutAnimationDemo : Component
{
    public override Element Render()
    {
        var (items, updateItems) = UseReducer(
            new List<string> { "Apple", "Banana", "Cherry" });
        var nextId = UseRef(3);

        return VStack(12,
            SubHeading("Layout Animation"),
            HStack(8,
                Button("Add Item", () => {
                    nextId.Current++;
                    updateItems(l => [.. l, $"Item {nextId.Current}"]);
                }),
                Button("Remove Last", () => updateItems(l =>
                    l.Count > 0 ? l.Take(l.Count - 1).ToList() : l))
            ),
            VStack(4, items.Select(item =>
                Text(item).Padding(8, 12).Background("#f0f0f0")
                    .CornerRadius(4).LayoutAnimation()
                    .WithKey($"item-{item}")
            ).ToArray())
        ).Padding(24);
    }
}
// </snippet:layout-animation>

// <snippet:connected-animation>
class ConnectedAnimationDemo : Component
{
    public override Element Render()
    {
        var (selected, setSelected) = UseState<string?>(null);

        if (selected is not null)
            return VStack(12,
                Button("Back to list", () => setSelected(null)),
                Text(selected)
                    .FontSize(28).Bold()
                    .ConnectedAnimation($"title-{selected}")
            ).Padding(24);

        var items = new[] { "Photos", "Music", "Videos" };
        return VStack(12,
            SubHeading("Connected Animation"),
            VStack(4,
                items.Select(item =>
                    Button(item, () => setSelected(item))
                        .ConnectedAnimation($"title-{item}")
                ).ToArray()
            )
        ).Padding(24);
    }
}
// </snippet:connected-animation>

// Main app
class AnimationApp : Component
{
    public override Element Render()
    {
        return ScrollView(
            VStack(24,
                Heading("Animation"),
                Component<OpacityDemo>(),
                Component<ScaleDemo>(),
                Component<TranslationDemo>(),
                Component<BackgroundDemo>(),
                Component<CombinedDemo>(),
                Component<LayoutAnimationDemo>(),
                Component<ConnectedAnimationDemo>()
            ).Padding(24)
        );
    }
}
