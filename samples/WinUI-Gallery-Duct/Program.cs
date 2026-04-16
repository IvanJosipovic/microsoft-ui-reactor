using Duct;
using Duct.Core;
using static Duct.UI;

DuctApp.Run<WinUIGalleryDuct.GalleryShell>("WinUI Gallery (Duct)", width: 1400, height: 900,
    configure: host => XamlInterop.Register(host.Reconciler));
