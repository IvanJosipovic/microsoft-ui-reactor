using System.ComponentModel;
using Duct;
using Duct.Core;
using Duct.Flex;
using Duct.PropertyGrid;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;

namespace Duct.EndToEnd.App.Fixtures;

internal static class PropertyGridFixtures
{
    // ════════════════════════════════════════════════════════════════
    //  Test models — cover all property grid object shapes
    // ════════════════════════════════════════════════════════════════

    // Simple mutable INPC model
    private class PersonModel : INotifyPropertyChanged
    {
        private string _name = "Alice";
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPC(nameof(Name)); } }
        }
        private int _age = 30;
        public int Age
        {
            get => _age;
            set { if (_age != value) { _age = value; OnPC(nameof(Age)); } }
        }
        private bool _active = true;
        public bool Active
        {
            get => _active;
            set { if (_active != value) { _active = value; OnPC(nameof(Active)); } }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPC(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // Model with categories and attributes
    private class CategorizedModel : INotifyPropertyChanged
    {
        private string _title = "Doc1";
        [PropertyCategory("General")]
        [PropertyDescription("The document title")]
        public string Title
        {
            get => _title;
            set { if (_title != value) { _title = value; OnPC(nameof(Title)); } }
        }

        private double _width = 100;
        [PropertyCategory("Layout")]
        [PropertyDisplayName("Width (px)")]
        public double Width
        {
            get => _width;
            set { if (_width != value) { _width = value; OnPC(nameof(Width)); } }
        }

        private double _height = 200;
        [PropertyCategory("Layout")]
        public double Height
        {
            get => _height;
            set { if (_height != value) { _height = value; OnPC(nameof(Height)); } }
        }

        [PropertyHidden]
        public int InternalId { get; set; } = 99;

        [PropertyReadOnly]
        public string Status { get; set; } = "OK";

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPC(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // Immutable record (Compose via constructor)
    private record Point2D(double X, double Y)
    {
        public override string ToString() => $"({X}, {Y})";
    }

    // Nested: mutable parent with immutable record child
    private class ShapeModel : INotifyPropertyChanged
    {
        private string _name = "Circle";
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPC(nameof(Name)); } }
        }

        private Point2D _position = new(10, 20);
        public Point2D Position
        {
            get => _position;
            set { if (_position != value) { _position = value; OnPC(nameof(Position)); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPC(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // Deep nesting: record inside record
    private record Theme(string Name, Point2D Origin);
    private record AppConfig(string Label, Theme Theme);

    // Enum for ComboBox
    private enum BlendMode { Normal, Multiply, Screen, Overlay }

    private class MaterialModel : INotifyPropertyChanged
    {
        private string _name = "Default";
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPC(nameof(Name)); } }
        }
        private BlendMode _blend = BlendMode.Normal;
        public BlendMode Blend
        {
            get => _blend;
            set { if (_blend != value) { _blend = value; OnPC(nameof(Blend)); } }
        }
        private double _opacity = 1.0;
        public double Opacity
        {
            get => _opacity;
            set { if (_opacity != value) { _opacity = value; OnPC(nameof(Opacity)); } }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPC(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // ════════════════════════════════════════════════════════════════
    //  Fixtures
    // ════════════════════════════════════════════════════════════════

    // 1. Basic mutable object renders all primitive editors
    internal class Reflection_MutableObject(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var model = new PersonModel();
            var registry = new TypeRegistry();

            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
            {
                ctx.UseObservable(model);
                return VStack(
                    PropertyGridDsl.PropertyGrid(model, registry),
                    Text($"Live: {model.Name},{model.Age},{model.Active}")
                );
            });
            await Harness.Render();

            H.Check("MutableObject_ShowsName", H.FindText("Name") is not null);
            H.Check("MutableObject_ShowsAge", H.FindText("Age") is not null);
            H.Check("MutableObject_ShowsActive", H.FindText("Active") is not null);
            H.Check("MutableObject_ShowsLiveValues",
                H.FindText("Live: Alice,30,True") is not null);
        }
    }

    // 2. Categories, hidden, read-only attributes
    internal class Reflection_Categorized(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var model = new CategorizedModel();
            var registry = new TypeRegistry();

            var host = new DuctHost(H.Window);
            host.Mount(_ => PropertyGridDsl.PropertyGrid(model, registry));
            await Harness.Render();

            H.Check("Categorized_ShowsGeneralCategory",
                H.FindTextContaining("General") is not null);
            H.Check("Categorized_ShowsLayoutCategory",
                H.FindTextContaining("Layout") is not null);
            H.Check("Categorized_ShowsTitle", H.FindText("Title") is not null);
            H.Check("Categorized_ShowsDisplayName",
                H.FindText("Width (px)") is not null);
            H.Check("Categorized_HidesInternalId",
                H.FindText("InternalId") is null);
            H.Check("Categorized_ShowsStatus", H.FindText("Status") is not null);
        }
    }

    // 3. Enum renders as ComboBox
    internal class Reflection_EnumEditor(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var model = new MaterialModel();
            var registry = new TypeRegistry();

            var host = new DuctHost(H.Window);
            host.Mount(_ => PropertyGridDsl.PropertyGrid(model, registry));
            await Harness.Render();

            H.Check("Enum_ShowsBlendProperty", H.FindText("Blend") is not null);

            // ComboBox should exist
            var comboBox = H.FindControl<ComboBox>(_ => true);
            H.Check("Enum_HasComboBox", comboBox is not null);
        }
    }

    // 4. Nested immutable record with expand/collapse
    internal class Nested_ImmutableRecord(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var model = new ShapeModel();
            var registry = new TypeRegistry();

            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
            {
                ctx.UseObservable(model);
                return VStack(
                    PropertyGridDsl.PropertyGrid(model, registry),
                    Text($"Pos: {model.Position}")
                );
            });
            await Harness.Render();

            H.Check("NestedRecord_ShowsName", H.FindText("Name") is not null);
            H.Check("NestedRecord_ShowsPosition", H.FindText("Position") is not null);
            H.Check("NestedRecord_ShowsPositionValue",
                H.FindTextContaining("(10, 20)") is not null);

            // Expand position — click the expand button on the Position row
            // The expand button content is a TextBlock with ▶ (U+25B6)
            var expandBtn = H.FindControl<Button>(b =>
                b.Content is string s && s.Contains("\u25B6") ||
                b.Content is TextBlock tb && tb.Text.Contains("\u25B6"));
            H.Check("NestedRecord_HasExpandButton", expandBtn is not null);

            if (expandBtn is not null)
            {
                var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(expandBtn);
                ((Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)
                    peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)).Invoke();
                await Harness.Render();

                H.Check("NestedRecord_ExpandedShowsX", H.FindText("X") is not null);
                H.Check("NestedRecord_ExpandedShowsY", H.FindText("Y") is not null);
            }
        }
    }

    // 5. Fully immutable root with OnRootChanged
    internal class Immutable_Root(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var registry = new TypeRegistry();

            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
            {
                var (point, setPoint) = ctx.UseState(new Point2D(5, 10));
                return VStack(
                    PropertyGridDsl.PropertyGrid(point, registry,
                        onRootChanged: obj => setPoint((Point2D)obj)),
                    Text($"Current: {point}")
                );
            });
            await Harness.Render();

            H.Check("ImmutableRoot_ShowsX", H.FindText("X") is not null);
            H.Check("ImmutableRoot_ShowsY", H.FindText("Y") is not null);
            H.Check("ImmutableRoot_ShowsInitialValue",
                H.FindText("Current: (5, 10)") is not null);
        }
    }

    // 6. Custom TypeMetadata with explicit editor
    internal class Custom_Editor(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var registry = new TypeRegistry();
            registry.Register<Point2D>(new TypeMetadata
            {
                Editor = (val, onChange) =>
                {
                    var p = (Point2D)val;
                    return Text($"Custom: {p.X},{p.Y}");
                },
                Decompose = val =>
                {
                    var p = (Point2D)val;
                    return new List<Duct.PropertyGrid.PropertyDescriptor>
                    {
                        new Duct.PropertyGrid.PropertyDescriptor { Name = "X", PropertyType = typeof(double),
                                GetValue = () => p.X, Order = 0 },
                        new Duct.PropertyGrid.PropertyDescriptor { Name = "Y", PropertyType = typeof(double),
                                GetValue = () => p.Y, Order = 1 },
                    };
                },
                Compose = (val, updates) =>
                {
                    var p = (Point2D)val;
                    var x = updates.TryGetValue("X", out var ux) ? (double)ux : p.X;
                    var y = updates.TryGetValue("Y", out var uy) ? (double)uy : p.Y;
                    return new Point2D(x, y);
                },
            });

            var model = new ShapeModel();

            var host = new DuctHost(H.Window);
            host.Mount(_ => PropertyGridDsl.PropertyGrid(model, registry));
            await Harness.Render();

            H.Check("CustomEditor_ShowsCustomText",
                H.FindTextContaining("Custom: 10,20") is not null);
        }
    }

    // 7. Switching targets doesn't crash
    internal class Target_Switching(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var person = new PersonModel();
            var material = new MaterialModel();
            var shape = new ShapeModel();
            var registry = new TypeRegistry();

            object target = person;
            Action? rerender = null;

            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
            {
                var (_, forceRender) = ctx.UseReducer(false);
                rerender = () => forceRender(v => !v);
                ctx.UseObservable(person);
                ctx.UseObservable(material);
                ctx.UseObservable(shape);
                return PropertyGridDsl.PropertyGrid(target, registry);
            });
            await Harness.Render();

            H.Check("Switching_PersonShows", H.FindText("Name") is not null);

            target = material;
            rerender?.Invoke();
            await Harness.Render();
            H.Check("Switching_MaterialShows", H.FindText("Blend") is not null);

            target = shape;
            rerender?.Invoke();
            await Harness.Render();
            H.Check("Switching_ShapeShows", H.FindText("Position") is not null);

            // Back to person
            target = person;
            rerender?.Invoke();
            await Harness.Render();
            H.Check("Switching_BackToPersonShows", H.FindText("Name") is not null);

            // Rapid switching
            for (int i = 0; i < 5; i++)
            {
                target = material; rerender?.Invoke(); await Harness.Render(50);
                target = person; rerender?.Invoke(); await Harness.Render(50);
                target = shape; rerender?.Invoke(); await Harness.Render(50);
            }
            await Harness.Render();
            H.Check("Switching_RapidNoCrash", true);
        }
    }

    // 8. Category expand/collapse
    internal class Category_ExpandCollapse(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var model = new CategorizedModel();
            var registry = new TypeRegistry();

            var host = new DuctHost(H.Window);
            host.Mount(_ => PropertyGridDsl.PropertyGrid(model, registry));
            await Harness.Render();

            // Initially all categories expanded
            H.Check("CategoryCollapse_TitleVisible", H.FindText("Title") is not null);
            H.Check("CategoryCollapse_WidthVisible",
                H.FindText("Width (px)") is not null);

            // Click "General" category header to collapse it
            var generalBtn = H.FindControl<Button>(b =>
            {
                // Category buttons have TextBlock children containing the category name
                if (b.Content is Microsoft.UI.Xaml.UIElement)
                {
                    var tb = FindTextInElement(b, "General");
                    return tb is not null;
                }
                return b.Content is string s && s.Contains("General");
            });
            H.Check("CategoryCollapse_FindGeneralButton", generalBtn is not null);

            if (generalBtn is not null)
            {
                var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(generalBtn);
                ((Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)
                    peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)).Invoke();
                await Harness.Render();

                // Title should be hidden (General category collapsed)
                H.Check("CategoryCollapse_TitleHidden", H.FindText("Title") is null);
                // Layout category still visible
                H.Check("CategoryCollapse_WidthStillVisible",
                    H.FindText("Width (px)") is not null);
            }
        }

        private static TextBlock? FindTextInElement(Microsoft.UI.Xaml.DependencyObject root, string text)
        {
            if (root is TextBlock tb && tb.Text?.Contains(text) == true) return tb;
            int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var found = FindTextInElement(
                    Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i), text);
                if (found is not null) return found;
            }
            return null;
        }
    }

    // 9. Deep nesting: record inside record
    internal class DeepNesting_RecordInRecord(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var config = new AppConfig("MyApp", new Theme("Dark", new Point2D(0, 0)));
            var registry = new TypeRegistry();

            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
            {
                var (c, setC) = ctx.UseState(config);
                return VStack(
                    PropertyGridDsl.PropertyGrid(c, registry,
                        onRootChanged: obj => setC((AppConfig)obj)),
                    Text($"Config: {c.Label}, {c.Theme.Name}, ({c.Theme.Origin.X},{c.Theme.Origin.Y})")
                );
            });
            await Harness.Render();

            H.Check("DeepNesting_ShowsLabel", H.FindText("Label") is not null);
            H.Check("DeepNesting_ShowsTheme", H.FindText("Theme") is not null);
            H.Check("DeepNesting_ShowsInitialConfig",
                H.FindTextContaining("Config: MyApp") is not null);
        }
    }

    // 10. INPC mutation re-renders the grid
    internal class INPC_ExternalMutation(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var person = new PersonModel { Name = "Alice", Age = 30 };
            var registry = new TypeRegistry();

            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
            {
                ctx.UseObservable(person);
                return VStack(
                    PropertyGridDsl.PropertyGrid(person, registry),
                    Text($"Live: {person.Name}"),
                    Button("MutateName", () => person.Name = "Bob")
                );
            });
            await Harness.Render();

            H.Check("INPC_InitialName", H.FindText("Live: Alice") is not null);

            H.ClickButton("MutateName");
            await Harness.Render();

            H.Check("INPC_MutatedName", H.FindText("Live: Bob") is not null);
        }
    }
}
