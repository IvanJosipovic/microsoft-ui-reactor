using Duct;
using Duct.Core;
using static Duct.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;

DuctApp.Run<AccessibilityApp>("Accessibility", width: 650, height: 700
#if DEBUG
    , preview: true
#endif
);

// <snippet:tier1-modifiers>
class Tier1Demo : Component
{
    public override Element Render()
    {
        return VStack(12,
            Text("Account Settings")
                .FontSize(24).Bold()
                .HeadingLevel(AutomationHeadingLevel.Level1),
            Text("Profile")
                .FontSize(18).SemiBold()
                .HeadingLevel(AutomationHeadingLevel.Level2),
            TextField("", _ => { }, placeholder: "Display name")
                .AutomationName("Display name")
                .TabIndex(1)
                .AccessKey("N"),
            Button("Save", () => { })
                .AutomationName("Save profile changes")
                .TabIndex(2)
                .AccessKey("S")
        ).Padding(24);
    }
}
// </snippet:tier1-modifiers>

// <snippet:tier2-modifiers>
class Tier2Demo : Component
{
    public override Element Render()
    {
        return VStack(12,
            TextField("", _ => { }, placeholder: "Search...")
                .AutomationName("Search products")
                .HelpText("Type a product name or SKU to filter results")
                .Width(300),
            VStack(8,
                Text("Revenue by Region").Bold(),
                Text("Bar chart placeholder").Opacity(0.5)
            ).FullDescription(
                "Bar chart showing Q1 revenue: East $4.2M, " +
                "West $3.8M, Central $2.1M")
             .Padding(16).Background("#f5f5f5").CornerRadius(8),
            Text("Decorative divider")
                .Opacity(0.2)
                .AccessibilityHidden()
        ).Padding(24);
    }
}
// </snippet:tier2-modifiers>

// <snippet:accessible-form>
class AccessibleFormDemo : Component
{
    public override Element Render()
    {
        var (name, setName) = UseState("");
        var (email, setEmail) = UseState("");
        var (agree, setAgree) = UseState(false);
        var valid = !string.IsNullOrWhiteSpace(name)
            && email.Contains('@') && agree;

        return VStack(12,
            Text("Create Account").FontSize(24).Bold()
                .HeadingLevel(AutomationHeadingLevel.Level1),
            TextField(name, setName, header: "Full Name")
                .AutomationName("Full name").Required().TabIndex(1),
            TextField(email, setEmail, header: "Email")
                .AutomationName("Email address").Required().TabIndex(2)
                .HelpText("We'll send a verification link"),
            CheckBox(agree, setAgree, label: "I accept the terms")
                .TabIndex(3),
            Button("Register", () => { })
                .Disabled(!valid).TabIndex(4).AccessKey("R")
        ).Landmark(AutomationLandmarkType.Form).Padding(24);
    }
}
// </snippet:accessible-form>

// <snippet:landmarks>
class LandmarksDemo : Component
{
    public override Element Render()
    {
        return VStack(16,
            HStack(8,
                Button("Home", () => { }),
                Button("Products", () => { }),
                Button("About", () => { })
            ).Landmark(AutomationLandmarkType.Navigation)
             .AutomationName("Main navigation"),

            VStack(12,
                Text("Dashboard")
                    .FontSize(20).Bold()
                    .HeadingLevel(AutomationHeadingLevel.Level1),
                Text("Welcome back. Here is your overview.")
            ).Landmark(AutomationLandmarkType.Main)
             .AutomationName("Main content"),

            TextField("", _ => { }, placeholder: "Search...")
                .AutomationName("Site search")
                .Landmark(AutomationLandmarkType.Search)
        ).Padding(24);
    }
}
// </snippet:landmarks>

// <snippet:heading-hierarchy>
class HeadingHierarchyDemo : Component
{
    public override Element Render()
    {
        return VStack(12,
            Text("Application Settings")
                .FontSize(24).Bold()
                .HeadingLevel(AutomationHeadingLevel.Level1),
            Text("Appearance")
                .FontSize(18).SemiBold()
                .HeadingLevel(AutomationHeadingLevel.Level2),
            Text("Choose your preferred theme and font size."),
            Text("Notifications")
                .FontSize(18).SemiBold()
                .HeadingLevel(AutomationHeadingLevel.Level2),
            Text("Email Alerts")
                .FontSize(15).SemiBold()
                .HeadingLevel(AutomationHeadingLevel.Level3),
            Text("Configure which emails you receive.")
        ).Padding(24);
    }
}
// </snippet:heading-hierarchy>

// Main app
class AccessibilityApp : Component
{
    public override Element Render()
    {
        return ScrollView(
            VStack(24,
                Heading("Accessibility"),
                Component<Tier1Demo>(),
                Component<Tier2Demo>(),
                Component<AccessibleFormDemo>(),
                Component<LandmarksDemo>(),
                Component<HeadingHierarchyDemo>()
            ).Padding(24)
        );
    }
}
