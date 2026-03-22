using Duct.Yoga;
using Microsoft.UI.Xaml.Controls;

namespace FlexPanelGallery.Pages;

public sealed partial class AlignItemsPage : Page
{
    public AlignItemsPage()
    {
        InitializeComponent();
    }

    private void AlignCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DemoPanel is null) return;
        if (AlignCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            DemoPanel.AlignItems = tag switch
            {
                "Stretch" => YogaAlign.Stretch,
                "FlexStart" => YogaAlign.FlexStart,
                "Center" => YogaAlign.Center,
                "FlexEnd" => YogaAlign.FlexEnd,
                "Baseline" => YogaAlign.Baseline,
                _ => YogaAlign.Stretch,
            };
        }
    }
}
