using Duct.Core.Localization;
using Xunit;

namespace Duct.Tests.Localization;

public class MessageCacheTests
{
    [Fact]
    public void Format_SimplePattern_ReturnsFormattedString()
    {
        var cache = new MessageCache();
        var result = cache.Format("en-US", "Hello, world!");
        Assert.Equal("Hello, world!", result);
    }

    [Fact]
    public void Format_WithArgs_InterpolatesValues()
    {
        var cache = new MessageCache();
        var args = new Dictionary<string, object> { ["name"] = "Alice" };
        var result = cache.Format("en-US", "Hello, {name}!", args);
        Assert.Equal("Hello, Alice!", result);
    }

    [Fact]
    public void GetFormatter_SameLocale_ReturnsCachedInstance()
    {
        var cache = new MessageCache();
        var f1 = cache.GetFormatter("en-US");
        var f2 = cache.GetFormatter("en-US");
        Assert.Same(f1, f2);
    }

    [Fact]
    public void GetFormatter_DifferentLocale_ReturnsDifferentInstances()
    {
        var cache = new MessageCache();
        var f1 = cache.GetFormatter("en-US");
        var f2 = cache.GetFormatter("fr-FR");
        Assert.NotSame(f1, f2);
    }

    [Fact]
    public void Flush_ClearsAllFormatters()
    {
        var cache = new MessageCache();
        var f1 = cache.GetFormatter("en-US");
        cache.Flush();
        var f2 = cache.GetFormatter("en-US");
        Assert.NotSame(f1, f2);
    }

    [Fact]
    public void FlushLocale_ClearsOnlyThatLocale()
    {
        var cache = new MessageCache();
        var enFormatter = cache.GetFormatter("en-US");
        var frFormatter = cache.GetFormatter("fr-FR");

        cache.Flush("en-US");

        var enAfter = cache.GetFormatter("en-US");
        var frAfter = cache.GetFormatter("fr-FR");

        Assert.NotSame(enFormatter, enAfter);  // flushed
        Assert.Same(frFormatter, frAfter);      // retained
    }

    [Fact]
    public async Task Format_ConcurrentAccess_NoExceptions()
    {
        var cache = new MessageCache();
        var exceptions = new List<Exception>();

        var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
        {
            try
            {
                var locale = i % 2 == 0 ? "en-US" : "fr-FR";
                var args = new Dictionary<string, object> { ["n"] = i };
                cache.Format(locale, "Item {n}", args);
            }
            catch (Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
            }
        }));

        await Task.WhenAll(tasks);
        Assert.Empty(exceptions);
    }
}
