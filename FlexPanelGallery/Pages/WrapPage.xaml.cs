using Duct.Yoga;
using Microsoft.UI.Xaml.Controls;

namespace FlexPanelGallery.Pages;

public sealed partial class WrapPage : Page
{
    public WrapPage()
    {
        InitializeComponent();
    }

    private void WrapCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DemoPanel is null) return;
        if (WrapCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            DemoPanel.Wrap = tag switch
            {
                "NoWrap" => YogaWrap.NoWrap,
                "Wrap" => YogaWrap.Wrap,
                "WrapReverse" => YogaWrap.WrapReverse,
                _ => YogaWrap.NoWrap,
            };
        }
    }
}
