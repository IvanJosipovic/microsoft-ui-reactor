using System.Diagnostics;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Navigation;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Reactor.Controls;
using static Microsoft.UI.Reactor.Factories;
using static Microsoft.UI.Reactor.Core.Theme;

class CounterDemo : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(0);
        var (step, setStep) = UseState(1);

        return VStack(12,
            Heading("Counter"),
            SubHeading($"Current count: {count}"),

            HStack(8,
                Button($"- {step}", () => setCount(count - step)),
                Button("Reset", () => setCount(0)).Disabled(count == 0),
                Button($"+ {step}", () => setCount(count + step))
            ),

            HStack(8,
                Factories.Text("Step size:"),
                Slider(step, 1, 10, v => setStep((int)v)).Width(200),
                Factories.Text($"{step}")
            ),

            // Conditional rendering — shows different messages based on count
            count switch
            {
                0 => Factories.Text("Try clicking the buttons!").Foreground(TertiaryText),
                > 0 and < 10 => Factories.Text("Going up..."),
                >= 10 and < 50 => Factories.Text("Getting bigger!").SemiBold(),
                >= 50 => Factories.Text("That's a LOT!").SemiBold(),
                < 0 and > -10 => Factories.Text("Going negative..."),
                _ => Factories.Text("Way down there!").SemiBold()
            }
        );
    }
}
