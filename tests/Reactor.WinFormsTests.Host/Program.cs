using Microsoft.UI.Reactor.Interop.WinForms;
using SWF = System.Windows.Forms;

namespace Microsoft.UI.Reactor.WinFormsTests.Host;

static class Program
{
    [STAThread]
    static void Main()
    {
        SWF.Application.EnableVisualStyles();
        SWF.Application.SetCompatibleTextRenderingDefault(false);

        XamlIslandBootstrap.Run(() =>
        {
            var form = new TestForm();
            form.Show();
            form.FormClosed += (_, _) => SWF.Application.Exit();
        });
    }
}
