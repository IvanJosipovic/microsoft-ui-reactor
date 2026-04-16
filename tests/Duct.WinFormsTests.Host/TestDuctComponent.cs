using Duct.Core;
using static Duct.UI;
using static Duct.Core.Theme;

namespace Duct.WinFormsTests.Host;

/// <summary>
/// A Duct component designed for E2E testing inside a XAML Island.
/// Every interactive control has an AutomationId so Appium can find it.
/// </summary>
class TestDuctComponent : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(0);
        var (text, setText) = UseState("");

        return Grid(["*"], ["*"],
            VStack(
                Text("Duct Island Content")
                    .FontSize(16)
                    .AutomationId("Duct_Title"),

                // Focusable text field — first Tab stop inside the island
                TextField(text, setText, placeholder: "Type in island")
                    .Width(250)
                    .AutomationId("Duct_TextField1")
                    .AutomationName("Island text field"),

                Text($"Text: {text}")
                    .AutomationId("Duct_TextDisplay"),

                // Focusable button — second Tab stop inside the island
                Button("Island Button", () => setCount(count + 1))
                    .AutomationId("Duct_Button1")
                    .AutomationName("Island button"),

                Text($"Count: {count}")
                    .AutomationId("Duct_CountDisplay"),

                // A second text field — third Tab stop
                TextField("", _ => { }, placeholder: "Second field")
                    .Width(250)
                    .AutomationId("Duct_TextField2")
                    .AutomationName("Island second field"),

                // Accessibility test targets
                Text("Status: Ready")
                    .LiveRegion(Microsoft.UI.Xaml.Automation.Peers.AutomationLiveSetting.Polite)
                    .AutomationId("Duct_LiveRegion"),

                Text("Island rendered successfully")
                    .AutomationId("Duct_RenderProof")

            ).Padding(16)
        ).Background(SolidBackground);
    }
}
