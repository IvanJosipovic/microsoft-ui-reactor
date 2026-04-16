using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;
using static WinUIGalleryDuct.SamplePageHost;

namespace WinUIGalleryDuct.ControlPages.MenusAndToolbars;

class MenuBarPage : Component
{
    public override Element Render()
    {
        var (lastAction, setLastAction) = UseState("(none)");

        return ScrollView(
            VStack(16,
                PageHeader("MenuBar",
                    "A horizontal bar that hosts a set of drop-down menus."),

                SampleCard("File/Edit/View Menus",
                    VStack(8,
                        MenuBar(
                            Menu("File",
                                MenuItem("New", () => setLastAction("New"), icon: "Page2"),
                                MenuItem("Open", () => setLastAction("Open"), icon: "OpenFile"),
                                MenuSeparator(),
                                MenuItem("Save", () => setLastAction("Save"), icon: "Save"),
                                MenuItem("Exit", () => setLastAction("Exit"))),
                            Menu("Edit",
                                MenuItem("Undo", () => setLastAction("Undo"), icon: "Undo"),
                                MenuItem("Redo", () => setLastAction("Redo"), icon: "Redo"),
                                MenuSeparator(),
                                MenuItem("Cut", () => setLastAction("Cut")),
                                MenuItem("Copy", () => setLastAction("Copy")),
                                MenuItem("Paste", () => setLastAction("Paste"))),
                            Menu("View",
                                MenuItem("Zoom In", () => setLastAction("Zoom In"), icon: "ZoomIn"),
                                MenuItem("Zoom Out", () => setLastAction("Zoom Out"), icon: "ZoomOut"))
                        ),
                        Text($"Last action: {lastAction}").Foreground(Theme.SecondaryText)
                    ),
                    @"MenuBar(
    Menu(""File"",
        MenuItem(""New"", () => {}, icon: ""Page2""),
        MenuItem(""Open"", () => {}, icon: ""OpenFile""),
        MenuSeparator(),
        MenuItem(""Save"", () => {}, icon: ""Save"")),
    Menu(""Edit"",
        MenuItem(""Undo"", () => {}, icon: ""Undo""),
        MenuItem(""Cut"", () => {}),
        MenuItem(""Copy"", () => {})))"),

                SampleCard("Nested SubMenus",
                    MenuBar(
                        Menu("Format",
                            MenuSubItem("Text Size",
                                MenuItem("Small", () => setLastAction("Small")),
                                MenuItem("Medium", () => setLastAction("Medium")),
                                MenuItem("Large", () => setLastAction("Large"))),
                            MenuSubItem("Alignment",
                                MenuItem("Left", () => setLastAction("Left")),
                                MenuItem("Center", () => setLastAction("Center")),
                                MenuItem("Right", () => setLastAction("Right"))))
                    ),
                    @"Menu(""Format"",
    MenuSubItem(""Text Size"",
        MenuItem(""Small"", () => {}),
        MenuItem(""Medium"", () => {})),
    MenuSubItem(""Alignment"",
        MenuItem(""Left"", () => {}),
        MenuItem(""Center"", () => {})))")
            ).Margin(36, 24, 36, 36)
        );
    }
}
