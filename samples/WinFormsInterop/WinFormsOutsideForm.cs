namespace WinFormsInterop.Sample;

/// <summary>
/// WinForms Form that demonstrates hosting Reactor/WinUI content via a XAML Island.
/// Left panel: native WinForms controls. Right panel: XAML Island with a Reactor component.
///
/// Designer-compatible: InitializeComponent creates and lays out all controls.
/// The XamlIslandControl.ComponentType property is set to SampleReactorComponent —
/// the designer serializes this as typeof(...) and the island creates the
/// ReactorHostControl automatically at runtime.
/// </summary>
partial class WinFormsOutsideForm : Form
{
    public WinFormsOutsideForm()
    {
        InitializeComponent();

        button.Click += (_, _) =>
            logList.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] WinForms button clicked");

        textBox.TextChanged += (_, _) =>
            logList.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] Text: {textBox.Text}");
    }
}
