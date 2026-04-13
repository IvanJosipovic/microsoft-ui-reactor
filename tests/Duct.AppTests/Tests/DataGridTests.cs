using Microsoft.VisualStudio.TestTools.UnitTesting;
using Duct.AppTests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace Duct.AppTests.Tests;

/// <summary>
/// E2E tests for DataGrid inline editing using Appium/WinAppDriver.
/// Exercises click-to-edit, real keyboard input, cross-row commit, and
/// same-row cell switching through the full WinUI accessibility pipeline.
/// </summary>
[TestClass]
public class DataGridTests : AppTestBase
{
    [ClassInitialize]
    public static void StartAppSession(TestContext context) => TestSession.AssemblyInit(context);

    [ClassCleanup]
    public static void StopAppSession() => TestSession.AssemblyCleanup();

    /// <summary>
    /// Click a cell to enter edit mode, type a new value, click a different
    /// row to commit, then click the second column to edit it, type, and
    /// press Enter to commit. Verifies the full editing pipeline through
    /// real mouse and keyboard input via WinAppDriver.
    /// </summary>
    [TestMethod]
    public void Interactive_DataGrid_ClickEditTabCommit()
    {
        NavigateToFixtureFresh("DataGrid_EditableGrid");

        // 1. Wait for grid data
        WaitForText("EditStatus", "Last edit: none");
        Assert.IsNotNull(WaitForName("Alice"), "'Alice' should be visible");
        Assert.IsNotNull(FindByName("Smith"), "'Smith' should be visible");

        // 2. Click "Alice" to start editing FirstName in row 1
        FindByName("Alice").Click();
        var editor1 = WaitForEditor("editor after clicking Alice");
        Assert.IsNotNull(editor1, "TextBox editor should appear after clicking cell");

        // 3. Clear and type new value
        editor1.Clear();
        editor1.SendKeys("Alicia");

        // 4. Click "Bob" (different row) to commit the FirstName edit
        Assert.IsNotNull(FindByName("Bob"), "'Bob' should be visible while editing");
        FindByName("Bob").Click();

        // 5. Verify first edit committed
        WaitForText("EditStatus", "Last edit: 1:Alicia,Smith");
        Assert.IsNotNull(WaitForName("Alicia"), "'Alicia' should be visible after commit");

        // 6. Click "Smith" to edit LastName in row 1
        Assert.IsNotNull(WaitForName("Smith"), "'Smith' should be visible");
        FindByName("Smith").Click();
        var editor2 = WaitForEditor("editor after clicking Smith");
        Assert.IsNotNull(editor2, "TextBox editor should appear for LastName");

        // 7. Clear and type
        editor2.Clear();
        editor2.SendKeys("Johnson");

        // 8. Press Enter to commit
        editor2.SendKeys(Keys.Enter);

        // 9. Verify second edit committed
        WaitForText("EditStatus", "Last edit: 1:Alicia,Johnson");
        Assert.IsNotNull(WaitForName("Alicia"), "'Alicia' should still be visible");
        Assert.IsNotNull(WaitForName("Johnson"), "'Johnson' should be visible after commit");
    }

    private WindowsElement? WaitForEditor(string context, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var found = FindAnyEditor();
            if (found is not null) return found;
            Thread.Sleep(100);
        }
        return null;
    }

    private WindowsElement? FindAnyEditor()
    {
        try
        {
            var eds = Session.FindElements(MobileBy.ClassName("TextBox"));
            if (eds.Count > 0) return eds[^1];
        }
        catch (WebDriverException) { }
        try
        {
            var eds = Session.FindElements(By.XPath("//Edit"));
            if (eds.Count > 0) return eds[^1];
        }
        catch (WebDriverException) { }
        return null;
    }

    private WindowsElement? WaitForName(string name, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            try { return Session.FindElement(MobileBy.Name(name)); }
            catch (WebDriverException) { Thread.Sleep(100); }
        }
        return null;
    }
}
