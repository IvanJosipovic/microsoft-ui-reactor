using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Layout;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.AppTests.Host.Fixtures;

internal static class LayoutFixtures
{
    internal static Element FlexRowDistribution(RenderContext ctx) =>
        FlexRow(
            Factories.Text("A").Flex(grow: 1).AutomationId("ItemA").Background("Red"),
            Factories.Text("B").Flex(grow: 2).AutomationId("ItemB").Background("Green"),
            Factories.Text("C").Flex(grow: 1).AutomationId("ItemC").Background("Blue")
        ).Width(600).Height(100);

    internal static Element FlexColumnWrap(RenderContext ctx)
    {
        var children = Enumerable.Range(0, 8)
            .Select(i => Factories.Text($"Box {i}").Width(80).Height(80).AutomationId($"Box{i}"))
            .ToArray();
        return new FlexElement(children)
        {
            Direction = FlexDirection.Column,
            Wrap = FlexWrap.Wrap,
        }.Width(400).Height(200);
    }

    internal static Element GridRowColumn(RenderContext ctx) =>
        Grid(["200", "*"], ["Auto", "*"],
            Factories.Text("TopLeft").Grid(row: 0, column: 0).AutomationId("TopLeft"),
            Factories.Text("TopRight").Grid(row: 0, column: 1).AutomationId("TopRight"),
            Factories.Text("BottomLeft").Grid(row: 1, column: 0).AutomationId("BottomLeft"),
            Factories.Text("BottomRight").Grid(row: 1, column: 1).AutomationId("BottomRight")
        ).Width(600).Height(400);

    internal static Element GridVsFlexStarSizing(RenderContext ctx) =>
        VStack(8,
            // Grid with 1* / 2* / 1* star columns
            Grid(["*", "2*", "*"], ["*"],
                Factories.Text("G1").Grid(row: 0, column: 0).AutomationId("G1").Background("LightCoral"),
                Factories.Text("G2").Grid(row: 0, column: 1).AutomationId("G2").Background("LightGreen"),
                Factories.Text("G3").Grid(row: 0, column: 2).AutomationId("G3").Background("LightBlue")
            ).Width(600).Height(80).AutomationId("StarGrid"),

            // FlexRow with grow 1 / 2 / 1 (basis:0 to match star behavior)
            FlexRow(
                Factories.Text("F1").Flex(grow: 1, basis: 0).AutomationId("F1").Background("LightCoral"),
                Factories.Text("F2").Flex(grow: 2, basis: 0).AutomationId("F2").Background("LightGreen"),
                Factories.Text("F3").Flex(grow: 1, basis: 0).AutomationId("F3").Background("LightBlue")
            ).Width(600).Height(80).AutomationId("StarFlex")
        ).Width(600).Height(400);
}
