using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace Microsoft.UI.Reactor.AppTests.Infrastructure;

/// <summary>
/// Base class for WinForms interop E2E tests.
/// Provides helpers for element lookup, waiting, focus verification, and Tab testing.
/// </summary>
public class WinFormsTestBase
{
    protected static WindowsDriver<WindowsElement> Session => WinFormsTestSession.Session;

    protected WindowsElement FindById(string automationId)
    {
        return Session.FindElement(MobileBy.AccessibilityId(automationId));
    }

    protected WindowsElement FindByName(string name)
    {
        return Session.FindElement(MobileBy.Name(name));
    }

    protected WindowsElement WaitForElement(string automationId, int timeoutMs = 5000)
    {
        var wait = new DefaultWait<WindowsDriver<WindowsElement>>(Session)
        {
            Timeout = TimeSpan.FromMilliseconds(timeoutMs),
            PollingInterval = TimeSpan.FromMilliseconds(100),
        };
        wait.IgnoreExceptionTypes(typeof(WebDriverException));

        return wait.Until(driver => driver.FindElement(MobileBy.AccessibilityId(automationId)));
    }

    protected void WaitForText(string automationId, string expectedText, int timeoutMs = 5000)
    {
        var wait = new DefaultWait<WindowsDriver<WindowsElement>>(Session)
        {
            Timeout = TimeSpan.FromMilliseconds(timeoutMs),
            PollingInterval = TimeSpan.FromMilliseconds(100),
        };
        wait.IgnoreExceptionTypes(typeof(WebDriverException));

        wait.Until(driver =>
        {
            var element = driver.FindElement(MobileBy.AccessibilityId(automationId));
            return element.Text == expectedText ? element : null;
        });
    }

    protected string WaitForTextContaining(string automationId, string substring, int timeoutMs = 5000)
    {
        var wait = new DefaultWait<WindowsDriver<WindowsElement>>(Session)
        {
            Timeout = TimeSpan.FromMilliseconds(timeoutMs),
            PollingInterval = TimeSpan.FromMilliseconds(100),
        };
        wait.IgnoreExceptionTypes(typeof(WebDriverException));

        string lastText = "";
        wait.Until(driver =>
        {
            var element = driver.FindElement(MobileBy.AccessibilityId(automationId));
            lastText = element.Text ?? "";
            return lastText.Contains(substring) ? element : null;
        });
        return lastText;
    }

    /// <summary>
    /// Sends a Tab key press to the currently focused element.
    /// </summary>
    protected void SendTab()
    {
        new Actions(Session).SendKeys(Keys.Tab).Perform();
    }

    /// <summary>
    /// Sends a Shift+Tab key press to the currently focused element.
    /// </summary>
    protected void SendShiftTab()
    {
        new Actions(Session)
            .KeyDown(Keys.Shift)
            .SendKeys(Keys.Tab)
            .KeyUp(Keys.Shift)
            .Perform();
    }

    /// <summary>
    /// Returns the currently focused element via a Desktop session query.
    /// WinAppDriver exposes the focused element through the active element API.
    /// </summary>
    protected WindowsElement GetFocusedElement()
    {
        return Session.SwitchTo().ActiveElement() as WindowsElement
            ?? throw new InvalidOperationException("Could not get active element");
    }

    /// <summary>
    /// Clicks an element by AccessibilityId first, falling back to Name.
    /// </summary>
    protected void ClickElement(string nameOrId)
    {
        try
        {
            var element = Session.FindElement(MobileBy.AccessibilityId(nameOrId));
            element.Click();
        }
        catch (WebDriverException)
        {
            var element = Session.FindElement(MobileBy.Name(nameOrId));
            element.Click();
        }
    }

    /// <summary>
    /// Returns the AutomationId of the currently focused element.
    /// Returns empty string if the focused element has no AutomationId or on error.
    /// </summary>
    protected string GetFocusedAutomationId()
    {
        try
        {
            var focused = GetFocusedElement();
            return focused.GetAttribute("AutomationId") ?? "";
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Polls until the focused element's AutomationId matches the expected value.
    /// Focus transitions (especially across WinForms ↔ XAML Island boundaries)
    /// are asynchronous — this avoids flaky assertions from checking too early.
    /// </summary>
    protected void AssertFocused(string expectedAutomationId, string step, int timeoutMs = 2000)
    {
        string actual = "";
        int elapsed = 0;
        const int pollMs = 50;

        while (elapsed < timeoutMs)
        {
            actual = GetFocusedAutomationId();
            if (actual == expectedAutomationId)
                return;
            Thread.Sleep(pollMs);
            elapsed += pollMs;
        }

        // Final check with assertion
        actual = GetFocusedAutomationId();
        Assert.AreEqual(expectedAutomationId, actual,
            $"[{step}] Expected focus on '{expectedAutomationId}' but found '{actual}'");
    }
}
