using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct.ControlPages.DialogsAndFlyouts;

class ContentDialogPage : Component
{
    public override Element Render()
    {
        var (showBasic, setShowBasic) = UseState(false);
        var (showConfirm, setShowConfirm) = UseState(false);
        var (result, setResult) = UseState("(none)");

        return ScrollView(
            VStack(16,
                PageHeader("ContentDialog",
                    "A modal dialog box that displays content and action buttons."),

                SampleCard("Basic Dialog",
                    VStack(8,
                        Button("Show Dialog", () => setShowBasic(true)),
                        ContentDialog("Welcome", Text("Thank you for using this app!"), "OK") with
                        {
                            IsOpen = showBasic,
                            OnClosed = _ => setShowBasic(false),
                        }
                    ),
                    @"Button(""Show Dialog"", () => setShow(true)),
ContentDialog(""Welcome"", Text(""Thank you!""), ""OK"") with {
    IsOpen = show,
    OnClosed = _ => setShow(false),
}"),

                SampleCard("Confirmation Dialog",
                    VStack(8,
                        Button("Delete Item", () => setShowConfirm(true)),
                        Text($"Last result: {result}").Foreground(Theme.SecondaryText),
                        ContentDialog("Confirm Delete",
                            Text("Are you sure you want to delete this item? This action cannot be undone."),
                            "Delete") with
                        {
                            IsOpen = showConfirm,
                            SecondaryButtonText = "Cancel",
                            OnClosed = r =>
                            {
                                setResult(r.ToString());
                                setShowConfirm(false);
                            },
                        }
                    ),
                    @"ContentDialog(""Confirm Delete"",
    Text(""Are you sure?""), ""Delete"") with {
    IsOpen = show,
    SecondaryButtonText = ""Cancel"",
    OnClosed = r => { setResult(r.ToString()); setShow(false); },
}")
            ).Margin(36, 24, 36, 36)
        );
    }
}
