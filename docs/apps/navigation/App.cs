using Duct;
using Duct.Core;
using Duct.Core.Navigation;
using static Duct.UI;
using Microsoft.UI.Xaml;

DuctApp.Run<NavigationApp>("Navigation", width: 800, height: 600
#if DEBUG
    , preview: true
#endif
);

// <snippet:route-enum>
enum Route { Home, Settings, Profile, Details }
// </snippet:route-enum>

// <snippet:basic-navigation>
class BasicNavDemo : Component
{
    public override Element Render()
    {
        var nav = UseNavigation(Route.Home);

        return VStack(12,
            SubHeading($"Current: {nav.CurrentRoute}"),
            Text($"Stack depth: {nav.Depth}"),
            HStack(8,
                Button("Home", () => nav.Navigate(Route.Home)),
                Button("Settings", () => nav.Navigate(Route.Settings)),
                Button("Profile", () => nav.Navigate(Route.Profile)),
                Button("Back", () => nav.GoBack())
                    .Disabled(!nav.CanGoBack)
            ),
            NavigationHost(nav, route => route switch
            {
                Route.Home => Text("Welcome home!").FontSize(18).Padding(16),
                Route.Settings => Text("Settings page").FontSize(18).Padding(16),
                Route.Profile => Text("Your profile").FontSize(18).Padding(16),
                _ => Text("Not found").Padding(16)
            })
        ).Padding(24);
    }
}
// </snippet:basic-navigation>

// <snippet:navigation-view>
class NavViewDemo : Component
{
    public override Element Render()
    {
        var nav = UseNavigation(Route.Home);
        return NavigationView(
            [
                NavItem("Home", icon: "Home", tag: "Home"),
                NavItem("Settings", icon: "Setting", tag: "Settings"),
                NavItem("Profile", icon: "Contact", tag: "Profile")
            ],
            content: NavigationHost(nav, route => route switch
            {
                Route.Home => VStack(12, Heading("Home"),
                    Text("Welcome to the app."),
                    Button("Go to Settings",
                        () => nav.Navigate(Route.Settings))).Padding(24),
                Route.Settings => VStack(12, Heading("Settings"),
                    Text("Configure your preferences."),
                    Button("Back", () => nav.GoBack())).Padding(24),
                Route.Profile => VStack(12, Heading("Profile"),
                    Text("View your profile info.")).Padding(24),
                _ => Text("Not found").Padding(24)
            })
        );
    }
}
// </snippet:navigation-view>

// <snippet:stack-operations>
class StackOperationsDemo : Component
{
    public override Element Render()
    {
        var nav = UseNavigation(Route.Home);

        return VStack(12,
            SubHeading($"Current: {nav.CurrentRoute}"),
            Text($"Back stack: {nav.BackStack.Count}"),
            Text($"Forward stack: {nav.ForwardStack.Count}"),
            HStack(8,
                Button("Navigate", () =>
                    nav.Navigate(Route.Settings)),
                Button("Replace", () =>
                    nav.Replace(Route.Profile)),
                Button("Reset", () =>
                    nav.Reset(Route.Home)),
                Button("Back", () => nav.GoBack())
                    .Disabled(!nav.CanGoBack),
                Button("Forward", () => nav.GoForward())
                    .Disabled(!nav.CanGoForward)
            ),
            NavigationHost(nav, route =>
                Text($"Page: {route}")
                    .FontSize(18).Padding(16))
        ).Padding(24);
    }
}
// </snippet:stack-operations>

// <snippet:lifecycle>
class LifecyclePage : Component
{
    public override Element Render()
    {
        var (log, updateLog) = UseReducer(new List<string>());

        UseNavigationLifecycle(
            onNavigatedTo: ctx =>
                updateLog(l => [.. l,
                    $"Arrived from {ctx.PreviousRoute}"]),
            onNavigatingFrom: ctx =>
                updateLog(l => [.. l,
                    $"Leaving to {ctx.TargetRoute}"]),
            onNavigatedFrom: ctx =>
                updateLog(l => [.. l,
                    $"Left for {ctx.TargetRoute}"])
        );

        return VStack(8,
            SubHeading("Lifecycle Events"),
            VStack(4,
                log.TakeLast(5).Select(entry =>
                    Text(entry).FontSize(12).Opacity(0.7)
                ).ToArray()
            )
        ).Padding(16);
    }
}
// </snippet:lifecycle>

// <snippet:tab-navigation>
class TabNavDemo : Component
{
    public override Element Render()
    {
        return TabView(
            Tab("Documents",
                VStack(12,
                    Text("Your documents appear here."),
                    Button("New Document", () => { })
                ).Padding(24)
            ),
            Tab("Recent",
                VStack(12,
                    Text("Recently opened files."),
                    Text("No recent files.").Opacity(0.5)
                ).Padding(24)
            ),
            Tab("Shared",
                VStack(12,
                    Text("Files shared with you."),
                    Text("Nothing shared yet.").Opacity(0.5)
                ).Padding(24)
            )
        );
    }
}
// </snippet:tab-navigation>

class NavigationApp : Component
{
    public override Element Render()
    {
        return ScrollView(
            VStack(24,
                Heading("Navigation"),
                Component<BasicNavDemo>(),
                Component<StackOperationsDemo>(),
                Component<TabNavDemo>()
            ).Padding(24)
        );
    }
}
