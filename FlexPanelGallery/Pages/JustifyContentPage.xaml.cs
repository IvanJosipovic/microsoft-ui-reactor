using Duct.Yoga;
using Microsoft.UI.Xaml.Controls;

namespace FlexPanelGallery.Pages;

public sealed partial class JustifyContentPage : Page
{
    public JustifyContentPage()
    {
        InitializeComponent();
    }

    private void JustifyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DemoPanel is null) return;
        if (JustifyCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            DemoPanel.JustifyContent = tag switch
            {
                "FlexStart" => YogaJustify.FlexStart,
                "Center" => YogaJustify.Center,
                "FlexEnd" => YogaJustify.FlexEnd,
                "SpaceBetween" => YogaJustify.SpaceBetween,
                "SpaceAround" => YogaJustify.SpaceAround,
                "SpaceEvenly" => YogaJustify.SpaceEvenly,
                _ => YogaJustify.FlexStart,
            };
        }
    }
}
