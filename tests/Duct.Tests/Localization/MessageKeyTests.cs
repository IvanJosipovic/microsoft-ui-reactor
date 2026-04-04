using Duct.Core.Localization;
using Xunit;

namespace Duct.Tests.Localization;

public class MessageKeyTests
{
    [Fact]
    public void Equality_SameNamespaceAndKey_AreEqual()
    {
        var a = new MessageKey("Common", "Save");
        var b = new MessageKey("Common", "Save");
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Equality_DifferentKey_NotEqual()
    {
        var a = new MessageKey("Common", "Save");
        var b = new MessageKey("Common", "Cancel");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equality_DifferentNamespace_NotEqual()
    {
        var a = new MessageKey("Common", "Save");
        var b = new MessageKey("Settings", "Save");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Hashing_EqualKeysHaveSameHashCode()
    {
        var a = new MessageKey("Cart", "ItemCount");
        var b = new MessageKey("Cart", "ItemCount");
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Hashing_WorksInDictionary()
    {
        var dict = new Dictionary<MessageKey, string>
        {
            [new MessageKey("Common", "Save")] = "Save",
            [new MessageKey("Common", "Cancel")] = "Cancel",
        };

        Assert.Equal("Save", dict[new MessageKey("Common", "Save")]);
        Assert.Equal("Cancel", dict[new MessageKey("Common", "Cancel")]);
    }

    [Fact]
    public void ToString_ReturnsNamespaceDotKey()
    {
        var key = new MessageKey("Settings", "Title");
        Assert.Equal("Settings.Title", key.ToString());
    }
}
