using Duct.Yoga;
using Microsoft.UI.Xaml.Controls;

namespace FlexPanelGallery.Pages;

public sealed partial class DirectionPage : Page
{
    public DirectionPage()
    {
        InitializeComponent();
    }

    private void DirectionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DemoPanel is null) return;
        if (DirectionCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            DemoPanel.Direction = tag switch
            {
                "Row" => YogaFlexDirection.Row,
                "RowReverse" => YogaFlexDirection.RowReverse,
                "Column" => YogaFlexDirection.Column,
                "ColumnReverse" => YogaFlexDirection.ColumnReverse,
                _ => YogaFlexDirection.Row,
            };
        }
    }
}
