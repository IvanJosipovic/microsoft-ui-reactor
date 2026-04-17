// Reactor AppTests Host — unified test host app for Appium/WinAppDriver UI tests.
// Normal mode: Shows fixture navigator UI for manual testing and Appium interactive tests.
// Self-test mode (--self-test): Runs all non-interactive fixtures in-process at CPU speed
//   using VisualTreeHelper, outputs TAP results to stdout, exits.

using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.AppTests.Host;
using Microsoft.UI.Reactor.AppTests.Host.SelfTest;

if (args.Contains("--self-test"))
{
    var filterIdx = Array.IndexOf(args, "--filter");
    if (filterIdx >= 0 && filterIdx + 1 < args.Length)
        SelfTestRunner.Filter = args[filterIdx + 1];
    SelfTestRunner.RunAll();
}
else
    ReactorApp.Run<TestHost>("Reactor Test Host", width: 1200, height: 800);
