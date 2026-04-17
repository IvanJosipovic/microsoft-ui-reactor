using Microsoft.UI.Reactor.Core;
using Xunit;

namespace Microsoft.UI.Reactor.Tests;

/// <summary>
/// Tests for Command and Command&lt;T&gt; records — equality, with expressions, IsEnabled logic.
/// Pure C# record tests, no WinUI thread needed.
/// </summary>
public class CommandTests
{
    // ════════════════════════════════════════════════════════════════
    //  Command — structural equality
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Command_Structural_Equality()
    {
        var a = new Command { Label = "Save" };
        var b = new Command { Label = "Save" };
        Assert.Equal(a, b);
    }

    [Fact]
    public void Command_Inequality_When_Label_Differs()
    {
        var a = new Command { Label = "Save" };
        var b = new Command { Label = "Open" };
        Assert.NotEqual(a, b);
    }

    // ════════════════════════════════════════════════════════════════
    //  Command — with expression
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Command_With_Creates_Modified_Copy()
    {
        var original = new Command { Label = "Save", Description = "Save the file" };
        var modified = original with { Label = "Save As" };

        Assert.Equal("Save As", modified.Label);
        Assert.Equal("Save the file", modified.Description); // unchanged
        Assert.NotEqual(original, modified);
    }

    // ════════════════════════════════════════════════════════════════
    //  Command — IsEnabled logic
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void IsEnabled_False_When_CanExecute_False()
    {
        var cmd = new Command { Label = "Cut", CanExecute = false };
        Assert.False(cmd.IsEnabled);
    }

    [Fact]
    public void IsEnabled_False_When_IsExecuting_True()
    {
        var cmd = new Command { Label = "Save", IsExecuting = true };
        Assert.False(cmd.IsEnabled);
    }

    [Fact]
    public void IsEnabled_False_When_CanExecute_False_And_IsExecuting_True()
    {
        var cmd = new Command { Label = "Save", CanExecute = false, IsExecuting = true };
        Assert.False(cmd.IsEnabled);
    }

    [Fact]
    public void IsEnabled_True_When_CanExecute_True_And_IsExecuting_False()
    {
        var cmd = new Command { Label = "Save" };
        Assert.True(cmd.CanExecute);
        Assert.False(cmd.IsExecuting);
        Assert.True(cmd.IsEnabled);
    }

    // ════════════════════════════════════════════════════════════════
    //  Command — defaults
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Command_Defaults()
    {
        var cmd = new Command { Label = "Test" };
        Assert.True(cmd.CanExecute);
        Assert.False(cmd.IsExecuting);
        Assert.True(cmd.IsEnabled);
        Assert.Null(cmd.Execute);
        Assert.Null(cmd.ExecuteAsync);
        Assert.Null(cmd.Icon);
        Assert.Null(cmd.Description);
        Assert.Null(cmd.Accelerator);
        Assert.Null(cmd.AccessKey);
    }

    // ════════════════════════════════════════════════════════════════
    //  Command<T> — equality and IsEnabled
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void CommandT_Structural_Equality()
    {
        var a = new Command<string> { Label = "Delete" };
        var b = new Command<string> { Label = "Delete" };
        Assert.Equal(a, b);
    }

    [Fact]
    public void CommandT_IsEnabled_Matches_Command_Logic()
    {
        var enabled = new Command<int> { Label = "Select" };
        Assert.True(enabled.IsEnabled);

        var cantExecute = new Command<int> { Label = "Select", CanExecute = false };
        Assert.False(cantExecute.IsEnabled);

        var executing = new Command<int> { Label = "Select", IsExecuting = true };
        Assert.False(executing.IsEnabled);

        var both = new Command<int> { Label = "Select", CanExecute = false, IsExecuting = true };
        Assert.False(both.IsEnabled);
    }

    [Fact]
    public void CommandT_With_Creates_Modified_Copy()
    {
        var original = new Command<string> { Label = "Delete", Description = "Remove item" };
        var modified = original with { Label = "Remove" };

        Assert.Equal("Remove", modified.Label);
        Assert.Equal("Remove item", modified.Description);
    }
}
