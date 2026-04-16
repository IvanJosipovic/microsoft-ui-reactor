# WinUI Gallery → Duct: Feature Gaps

This document lists WinUI Gallery controls, features, and XAML concepts that **cannot be directly ported** to the Duct declarative framework, along with the recommended Duct alternative where one exists.

---

## Controls with No Duct Equivalent

### InkCanvas / InkToolbar
**WinUI:** Rich inking surface with pen, pencil, and eraser tools.
**Duct Alternative:** None. Inking requires platform ink infrastructure. Use `.Set()` to embed a native `InkCanvas` inside a Duct tree if needed, but there is no declarative wrapper.

### AppNotification (Toast Notifications)
**WinUI:** System-level toast notifications via `AppNotificationManager`.
**Duct Alternative:** None. Toast notifications are OS-level APIs outside the UI tree. Call the `AppNotificationManager` API directly from hook callbacks or event handlers.

### ConnectedAnimation
**WinUI:** Fluid page-to-page element transitions using `ConnectedAnimationService`.
**Duct Alternative:** None. Connected animations require coordination between XAML pages and the composition layer. Not expressible in the current Duct model.

### XamlDirect
**WinUI:** Low-level API for high-performance XAML object creation bypassing the type system.
**Duct Alternative:** None. Duct's reconciler already optimizes element creation; XamlDirect's perf benefits are subsumed by the virtual element tree.

---

## Windowing & System APIs

### AppWindow (Multi-Window)
**WinUI:** Create and manage multiple application windows.
**Duct Alternative:** None. Duct renders a single component tree per window. Multi-window apps must create separate `DuctHost` instances per window using the hosting API directly.

### Clipboard APIs
**WinUI:** `DataPackage`, `Clipboard.SetContent()`, `Clipboard.GetContent()`.
**Duct Alternative:** None. Clipboard is a platform API, not a UI element. Call `Windows.ApplicationModel.DataTransfer.Clipboard` directly from event handlers.

### FilePicker / FolderPicker
**WinUI:** System file/folder picker dialogs.
**Duct Alternative:** None. Pickers are OS dialogs. Call `FileOpenPicker` / `FolderPicker` directly from button click handlers.

---

## XAML Concepts Replaced by C# Patterns

### DataTemplate / ControlTemplate
**WinUI:** Declarative XAML templates for data presentation and control styling.
**Duct Alternative:** **C# functions.** Write a method like `Element RenderItem(MyModel item) => HStack(...)` and call it inline. Duct's composable element model replaces templates entirely.

### x:Bind / {Binding}
**WinUI:** Declarative data binding expressions in XAML markup.
**Duct Alternative:** **Hooks (`UseState`, `UseReducer`).** State is managed via hooks and flows through `Render()` naturally. No binding expressions needed — values are captured in closures.

### VisualStateManager / AdaptiveTriggers
**WinUI:** State-based visual changes and responsive layout triggers.
**Duct Alternative:** **C# conditionals and hooks.** Use `if`/`switch` in `Render()` based on state or window size. Example: `var layout = width > 800 ? WideLayout() : NarrowLayout();`

### Custom Styles / ResourceDictionaries
**WinUI:** XAML resource dictionaries for sharing styles and theme resources.
**Duct Alternative:** **`Theme` API + `.Set()` modifier.** Use `Theme.Accent`, `Theme.CardBackground`, etc. for theme-aware colors. For custom styling, use `.Set(el => { ... })` to configure native properties. Share styles as C# helper methods.

---

## Visual Effects (Partial Support)

### ThemeAnimation / Storyboard
**WinUI:** Declarative animations via `Storyboard`, `DoubleAnimation`, and theme transitions.
**Duct Alternative:** **Limited.** Duct supports implicit theme transitions on supported properties. For custom animations, use `.Set()` to access the composition layer directly. No declarative animation DSL exists.

### Reveal / Composition Effects
**WinUI:** Reveal highlight, composition visual effects, blur, shadows.
**Duct Alternative:** **Partial via `.Set()`.** Use `.Set(el => { ... })` to access the element's visual and apply composition effects. `AcrylicBrush()` provides built-in acrylic support. Other effects require manual composition API usage.

---

## Summary Table

| Feature | Duct Status | Alternative |
|---|---|---|
| InkCanvas / InkToolbar | ❌ Not available | Embed native via `.Set()` |
| AppNotification (Toast) | ❌ Not available | Call OS API directly |
| AppWindow (Multi-Window) | ❌ Not available | Multiple `DuctHost` instances |
| Clipboard APIs | ❌ Not available | Call `Clipboard` API directly |
| FilePicker / FolderPicker | ❌ Not available | Call picker API directly |
| ConnectedAnimation | ❌ Not available | No equivalent |
| XamlDirect | ❌ Not needed | Reconciler handles perf |
| DataTemplate | ✅ Replaced | C# functions |
| x:Bind / {Binding} | ✅ Replaced | Hooks (`UseState`) |
| VisualStateManager | ✅ Replaced | C# conditionals |
| Styles / ResourceDictionary | ✅ Replaced | `Theme` API + `.Set()` |
| ThemeAnimation / Storyboard | ⚠️ Limited | Implicit transitions, `.Set()` |
| Reveal / Composition | ⚠️ Partial | `AcrylicBrush()`, `.Set()` |
