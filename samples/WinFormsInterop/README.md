# WinForms Interop Sample

Demonstrates bidirectional hosting between Duct/WinUI and WinForms.

## Usage

```
dotnet run                   # WinForms-primary mode (default)
dotnet run -- --duct         # Duct-primary mode
```

Both modes open two windows:
1. **Duct outside** — Duct/WinUI renders the UI, with a WinForms DataGridView embedded
2. **WinForms outside** — WinForms Form with native controls, plus a XAML Island hosting a Duct component

## Status Matrix

| | **WinForms hosts Duct** (XAML Island) | **Duct hosts WinForms** (child HWND) |
|---|---|---|
| **WinForms-primary** (`--winforms`) | Working | Working |
| **Duct-primary** (`--duct`) | Working | **Not working** |

### What works

- **WinForms hosts Duct (both modes):** `XamlIslandControl` wraps `DesktopWindowXamlSource` to host a `DuctHostControl` inside a WinForms Form. Works in both modes because `Application.Start()` provides the XAML runtime regardless of who "owns" the process.

- **Duct hosts WinForms (WinForms-primary):** The "Duct outside" window is a WinForms Form with a full-bleed XAML Island. The `WinFormsHostElement` reparents the WinForms control as a `WS_EX_LAYERED` child HWND of the Form. This works because WinForms Forms have a **GDI redirection bitmap** — standard Win32 surface where child HWNDs can paint via GDI.

### What doesn't work (and why)

- **Duct hosts WinForms (Duct-primary):** The WinUI Window uses `WS_EX_NOREDIRECTIONBITMAP` — there is **no GDI surface**. The entire window is rendered by DirectComposition. WinForms controls paint via GDI (`WM_PAINT`), but the compositor overwrites their content every frame. The DataGridView *does* paint (visible briefly during resize), but the compositor immediately covers it.

  This is a fundamental architectural mismatch between WinUI's compositor-only rendering and Win32/GDI child windows. There is no supported workaround — Microsoft's open [ThousandIslands proposal](https://github.com/microsoft/microsoft-ui-xaml/issues/10050) acknowledges this gap.

  Attempted approaches that don't work:
  - `WS_EX_LAYERED` + `SetLayeredWindowAttributes` — compositor still overpaints
  - Parenting to DesktopChildSiteBridge child HWND — same result
  - `SetWindowPos(HWND_TOP)` on every frame — briefly visible, then covered
  - Owned overlay Form (borderless tool window) — WinUI window still paints over it
  - Removing placeholder background — compositor surface itself is opaque

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
                Microsoft.UI.Xaml.Application.Current.Exit();
        });
    }
}
```

**3. Drop an `XamlIslandControl` into your form** (designer or code-behind):
```csharp
using Duct;
using Duct.Interop.WinForms;

// In Form1 constructor or Load handler:
var island = new XamlIslandControl { Dock = DockStyle.Fill };
var ductHost = new DuctHostControl();
ductHost.Mount(new MyDuctComponent());
island.XamlContent = ductHost;
Controls.Add(island);
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
Duct.Interop.WinForms/              Library (could move into Duct proper)
  XamlIslandControl.cs               WinForms Control -> DesktopWindowXamlSource
  XamlIslandBootstrap.cs             Initialize WinAppSDK for WinForms-primary apps
  WinFormsHostElement.cs             Duct Element + WinFormsHostBridge registration

samples/WinFormsInterop/            This sample
  Program.cs                         CLI parsing, both bootstrap modes
  DuctOutsideComponent.cs            Duct component with embedded WinForms DataGridView
  WinFormsOutsideForm.cs             WinForms Form with embedded Duct island
  SampleDuctComponent.cs             Duct component shown inside the XAML Island
```

## Key Findings

1. **WinForms inside Duct (via XAML Island) works well.** `DesktopWindowXamlSource` + `DuctHostControl` is the officially supported path. Wrap content in a `Grid` with `ApplicationPageBackgroundThemeBrush` since islands don't provide a background.

2. **Duct inside WinForms works only when the parent has a GDI surface.** The `WS_EX_LAYERED` child HWND approach works when the parent is a WinForms Form (or any window with a GDI redirection bitmap). It does not work when the parent is a WinUI Window (`WS_EX_NOREDIRECTIONBITMAP`).

3. **`Application.Start()` is required for XAML Islands.** Creating `new Application()` directly fails with `RPC_E_WRONG_THREAD`. The native XAML runtime must be initialized via `Application.Start()`, which also provides the message loop. WinForms windows work inside this loop since they're standard Win32 windows.

4. **The message loop owner doesn't matter for XAML Islands.** Both WinUI's `Application.Start()` and WinForms' `Application.Run()` are standard Win32 message pumps. XAML Islands work in either.

5. **The message loop owner DOES matter for child HWND hosting.** WinUI Windows use `WS_EX_NOREDIRECTIONBITMAP` (compositor-only, no GDI surface), which prevents GDI child windows from rendering. This is an architectural limitation, not a bug.
