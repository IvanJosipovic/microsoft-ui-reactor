# WinForms Interop Sample

Demonstrates hosting Duct/WinUI content inside a WinForms application via XAML Islands (`DesktopWindowXamlSource`). This is the officially supported interop direction — WinForms on the outside, Duct/WinUI content rendered inside.

## Usage

```
dotnet run
```

Opens a single WinForms window with:
- **Left panel:** native WinForms controls (label, textbox, button, event log)
- **Right panel:** XAML Island hosting a Duct component (counter, text input, slider, rounded boxes)

Tab navigation works across both panels.

## Quick Start: Add Duct to an Existing WinForms App

Starting from a standard `dotnet new winforms` app:

**Before** — vanilla WinForms (`Program.cs`):
```csharp
namespace MyApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}
```

**After** — three changes to host a Duct component:

**1. Add to `.csproj`:**
```xml
    <UseWinUI>true</UseWinUI>
    <WindowsPackageType>None</WindowsPackageType>

    <PackageReference Include="Microsoft.WindowsAppSDK" Version="2.0.0-experimental6" />
    <ProjectReference Include="path\to\Duct.Interop.WinForms.csproj" />
    <ProjectReference Include="path\to\Duct.csproj" />
```

**2. Change `Program.cs`** — wrap `Application.Run` with `XamlIslandBootstrap.Run`:
```csharp
using Duct.Interop.WinForms;

namespace MyApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        XamlIslandBootstrap.Run(() =>              // replaces Application.Run —
        {                                          // initializes WinUI runtime,
            var form = new Form1();                // then runs your app normally
            form.Show();
            form.FormClosed += (_, _) =>
                System.Windows.Forms.Application.Exit();  // exits the WinForms loop
        });
    }
}
```

**3. Drop an `XamlIslandControl` into your form:**

In the designer: drag `XamlIslandControl` onto your form, set `Dock = Fill`, and set
`ComponentType` to your Duct component in the Properties grid (under the "Duct" category).
The designer shows a placeholder; the real WinUI content appears at runtime.

In code (e.g., `InitializeComponent` or a `.Designer.cs` file):
```csharp
using Duct.Interop.WinForms;

var island = new XamlIslandControl
{
    Dock = DockStyle.Fill,
    ComponentType = typeof(MyDuctComponent),
};
Controls.Add(island);
```

The `ComponentType` property is fully designer-safe — it serializes as `typeof(...)` and
the `DuctHostControl` is created automatically at runtime.

For advanced scenarios (custom DuctHostControl configuration, props, etc.) use
`ContentFactory` instead:
```csharp
island.ContentFactory = () => new DuctHostControl(new MyDuctComponent());
```

**4. Write the Duct component:**
```csharp
using Duct;
using Duct.Core;
using static Duct.UI;
using static Duct.Core.Theme;

class MyDuctComponent : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(0);

        // Grid + Background fills the island — XAML Islands don't provide
        // a background or stretch content like a WinUI Window does.
        return Grid(["*"], ["*"],
            VStack(
                Text("Hello from Duct!").FontSize(24),
                Button($"Clicked {count} times", () => setCount(count + 1))
            ).Padding(24)
        ).Background(SolidBackground);
    }
}
```

That's it. The `XamlIslandControl` works anywhere in your WinForms layout — panels, split containers, tab pages, etc.

## Architecture

```
Duct.Interop.WinForms/              Library
  XamlIslandControl.cs               WinForms Control -> DesktopWindowXamlSource
  XamlIslandBootstrap.cs             Initialize WinAppSDK for WinForms-primary apps
  DuctComponentTypeConverter.cs      TypeConverter for ComponentType property grid

samples/WinFormsInterop/            This sample
  Program.cs                         Bootstrap via XamlIslandBootstrap.Run
  WinFormsOutsideForm.cs             WinForms Form with WinForms + Duct side-by-side
  SampleDuctComponent.cs             Duct component shown inside the XAML Island
```

## Key Notes

1. **`Application.Start()` is required for XAML Islands.** The native XAML runtime must be initialized via `Application.Start()`, which `XamlIslandBootstrap.Run` handles. Creating `new Application()` directly fails with `RPC_E_WRONG_THREAD`.

2. **Exit via `System.Windows.Forms.Application.Exit()`.** The WinForms message loop owns the process — exit through WinForms, not `Microsoft.UI.Xaml.Application.Current.Exit()`. The bootstrap handles the XAML runtime shutdown automatically after the WinForms loop exits.

3. **Wrap content in a Grid with a background.** XAML Islands don't stretch content or provide a background like a WinUI Window does. Use `Grid(["*"], ["*"], ...).Background(SolidBackground)` in your root component.

4. **Tab navigation works across the boundary.** `XamlIslandControl` bridges `TakeFocusRequested` and `NavigateFocus` so Tab/Shift+Tab cycles between WinForms controls and WinUI controls inside the island.
