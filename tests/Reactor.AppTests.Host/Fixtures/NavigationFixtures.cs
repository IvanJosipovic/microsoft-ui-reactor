using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Navigation;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.AppTests.Host.Fixtures;

internal static class NavigationFixtures
{
    // ── Existing: tab switching with buttons (UseState) ──────────────────

    internal class TabSwitchingComponent : Component
    {
        public override Element Render()
        {
            var (tab, setTab) = UseState("Home");

            return VStack(
                HStack(
                    Button("Home", () => setTab("Home"))
                        .Disabled(tab == "Home").AutomationId("HomeTab"),
                    Button("Settings", () => setTab("Settings"))
                        .Disabled(tab == "Settings").AutomationId("SettingsTab"),
                    Button("About", () => setTab("About"))
                        .Disabled(tab == "About").AutomationId("AboutTab")
                ),
                Border(
                    tab switch
                    {
                        "Home" => VStack(
                            Factories.Text("Welcome Home").AutomationId("HomeTitle"),
                            Factories.Text("This is the home page.").AutomationId("HomeContent")),
                        "Settings" => VStack(
                            Factories.Text("Settings Page").AutomationId("SettingsTitle"),
                            Factories.Text("Configure your preferences.").AutomationId("SettingsContent")),
                        "About" => VStack(
                            Factories.Text("About Page").AutomationId("AboutTitle"),
                            Factories.Text("Version 1.0").AutomationId("AboutVersion")),
                        _ => Empty()
                    }
                ).Padding(16).AutomationId("TabContent")
            );
        }
    }

    internal static Element TabSwitching(RenderContext ctx) =>
        Component<TabSwitchingComponent>();

    // ── Multi-level navigation (UseNavigation + NavigationHost) ──────────

    abstract record NavRoute;
    sealed record NavHome : NavRoute;
    sealed record NavList : NavRoute;
    sealed record NavDetail(int Id) : NavRoute;
    sealed record NavRelated(int SourceId) : NavRoute;

    internal class MultiLevelNavComponent : Component
    {
        public override Element Render()
        {
            var nav = UseNavigation<NavRoute>(new NavHome());

            return VStack(
                HStack(8,
                    Button("Back", () => nav.GoBack())
                        .Disabled(!nav.CanGoBack)
                        .AutomationId("BackBtn"),
                    Factories.Text($"Depth: {nav.Depth}").AutomationId("NavDepth")
                ),
                NavigationHost(nav, route => route switch
                {
                    NavHome => VStack(
                        Factories.Text("Home").AutomationId("PageTitle"),
                        Button("Go to List", () => nav.Navigate(new NavList()))
                            .AutomationId("GoListBtn")),
                    NavList => VStack(
                        Factories.Text("Item List").AutomationId("PageTitle"),
                        Button("View Detail #1", () => nav.Navigate(new NavDetail(1)))
                            .AutomationId("GoDetail1Btn"),
                        Button("View Detail #2", () => nav.Navigate(new NavDetail(2)))
                            .AutomationId("GoDetail2Btn")),
                    NavDetail d => VStack(
                        Factories.Text($"Detail #{d.Id}").AutomationId("PageTitle"),
                        Button("Related Items", () => nav.Navigate(new NavRelated(d.Id)))
                            .AutomationId("GoRelatedBtn")),
                    NavRelated r => VStack(
                        Factories.Text($"Related to #{r.SourceId}").AutomationId("PageTitle"),
                        Button("Back to Home", () => nav.Reset(new NavHome()))
                            .AutomationId("ResetBtn")),
                    _ => Factories.Text("Unknown")
                }) with { Transition = NavigationTransition.None }
            );
        }
    }

    internal static Element MultiLevelNav(RenderContext ctx) =>
        Component<MultiLevelNavComponent>();

    // ── Navigation with guard ───────────────────────────────────────────

    internal class NavGuardComponent : Component
    {
        public override Element Render()
        {
            var nav = UseNavigation<string>("page-a");

            return VStack(
                HStack(8,
                    Button("Go to A", () => nav.Navigate("page-a"))
                        .AutomationId("GoABtn"),
                    Button("Go to B", () => nav.Navigate("page-b"))
                        .AutomationId("GoBBtn"),
                    Factories.Text($"Current: {nav.CurrentRoute}").AutomationId("CurrentRoute")
                ),
                NavigationHost(nav, route => route switch
                {
                    "page-a" => Component<PageA>(),
                    "page-b" => Component<PageB>(),
                    _ => Factories.Text("Unknown")
                }) with { Transition = NavigationTransition.None }
            );
        }
    }

    class PageA : Component
    {
        public override Element Render()
        {
            return VStack(
                Factories.Text("Page A").AutomationId("PageTitle"),
                Factories.Text("No guard on this page.").AutomationId("GuardStatus")
            );
        }
    }

    class PageB : Component
    {
        public override Element Render()
        {
            var (dirty, setDirty) = UseState(false);

            UseNavigationLifecycle(
                onNavigatingFrom: ctx =>
                {
                    if (dirty) ctx.Cancel();
                });

            return VStack(
                Factories.Text("Page B").AutomationId("PageTitle"),
                Factories.Text(dirty ? "Guard: ACTIVE" : "Guard: inactive").AutomationId("GuardStatus"),
                Button("Toggle Guard", () => setDirty(!dirty)).AutomationId("ToggleGuardBtn")
            );
        }
    }

    internal static Element NavGuard(RenderContext ctx) =>
        Component<NavGuardComponent>();

    // ── NavigationView + NavigationHost integration ─────────────────────

    abstract record ViewRoute;
    sealed record ViewHome : ViewRoute;
    sealed record ViewSettings : ViewRoute;
    sealed record ViewAbout : ViewRoute;

    internal class NavViewIntegrationComponent : Component
    {
        public override Element Render()
        {
            var nav = UseNavigation<ViewRoute>(new ViewHome());

            return (NavigationView(
                [
                    NavItem("Home", tag: "home"),
                    NavItem("Settings", tag: "settings"),
                    NavItem("About", tag: "about"),
                ],
                NavigationHost(nav, route => route switch
                {
                    ViewHome => Factories.Text("Home Content").AutomationId("PageContent"),
                    ViewSettings => Factories.Text("Settings Content").AutomationId("PageContent"),
                    ViewAbout => Factories.Text("About Content").AutomationId("PageContent"),
                    _ => Factories.Text("Unknown")
                }) with { Transition = NavigationTransition.None }
            ).WithNavigation(nav,
                route => route switch
                {
                    ViewHome => "home",
                    ViewSettings => "settings",
                    ViewAbout => "about",
                    _ => null,
                },
                tag => tag switch
                {
                    "settings" => new ViewSettings(),
                    "about" => new ViewAbout(),
                    _ => new ViewHome(),
                }) with
            {
                IsSettingsVisible = false,
            }).AutomationId("NavViewHost");
        }
    }

    internal static Element NavViewIntegration(RenderContext ctx) =>
        Component<NavViewIntegrationComponent>();
}
