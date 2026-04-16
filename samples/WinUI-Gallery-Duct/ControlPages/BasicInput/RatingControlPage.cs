using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct.ControlPages.BasicInput;

class RatingControlPage: Component
{
    public override Element Render()
    {
        var (rating, setRating) = UseState(3.0);

        return ScrollView(VStack(16,
            PageHeader("RatingControl", "A control that lets users provide a star rating."),

            SampleCard("Basic RatingControl",
                VStack(8,
                    RatingControl(rating, v => setRating(v)),
                    Text($"Rating: {rating:F1}").Foreground(Theme.SecondaryText)),
                sourceCode: @"
RatingControl(rating, v => setRating(v))
"),

            SampleCard("Read-Only Rating",
                RatingControl(4.0).ReadOnly(),
                sourceCode: @"
RatingControl(4.0).ReadOnly()
"),

            SampleCard("Custom Max Rating",
                RatingControl(rating, v => setRating(v)).MaxRating(10),
                sourceCode: @"
RatingControl(rating, v => setRating(v)).MaxRating(10)
")
        ).Margin(36, 24, 36, 36));
    }
}
