using Microsoft.UI.Reactor.Interop.WinForms;
using WinForms = System.Windows.Forms;

namespace WinFormsInterop.Sample;

/// <summary>
/// WinForms app that hosts a Reactor/WinUI component via XAML Island.
///
/// Demonstrates the supported interop path: WinForms on the outside,
/// Reactor/WinUI content hosted inside via DesktopWindowXamlSource.
/// </summary>
static class Program
{
    [STAThread]
    static void Main()
    {
        WinForms.Application.EnableVisualStyles();
        WinForms.Application.SetCompatibleTextRenderingDefault(false);

        XamlIslandBootstrap.Run(() =>
        {
            var form = new WinFormsOutsideForm();
            form.Show();
            form.FormClosed += (_, _) => WinForms.Application.Exit();
        });
    }
}
