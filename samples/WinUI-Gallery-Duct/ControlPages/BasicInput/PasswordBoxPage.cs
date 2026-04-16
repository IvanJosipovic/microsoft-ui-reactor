using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct.ControlPages.BasicInput;

class PasswordBoxPage: Component
{
    public override Element Render()
    {
        var (password, setPassword) = UseState("");
        var (revealPwd, setRevealPwd) = UseState("");

        return ScrollView(VStack(16,
            PageHeader("PasswordBox", "A text input that conceals typed characters for secure entry."),

            SampleCard("Basic PasswordBox",
                VStack(8,
                    PasswordBox(password, p => setPassword(p), "Enter password"),
                    Text($"Length: {password.Length} characters").Foreground(Theme.SecondaryText)),
                sourceCode: @"
PasswordBox(password, p => setPassword(p), ""Enter password"")
"),

            SampleCard("PasswordBox with Reveal Button",
                PasswordBox(revealPwd, p => setRevealPwd(p), "Password")
                    .Set(pb => pb.PasswordRevealMode = PasswordRevealMode.Peek),
                sourceCode: @"
PasswordBox(revealPwd, p => setRevealPwd(p), ""Password"")
    .Set(pb => pb.PasswordRevealMode = PasswordRevealMode.Peek)
")
        ).Margin(36, 24, 36, 36));
    }
}
