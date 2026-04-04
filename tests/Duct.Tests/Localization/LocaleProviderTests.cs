using Duct.Core;
using Duct.Core.Localization;
using Microsoft.UI.Xaml;
using Xunit;

namespace Duct.Tests.Localization;

public class LocaleProviderTests
{
    [Fact]
    public void UseIntl_WithoutProvider_ReturnsDefaultAccessor()
    {
        var ctx = new RenderContext();
        ctx.BeginRender(() => { });

        var t = ctx.UseIntl();

        Assert.NotNull(t);
        Assert.NotEmpty(t.Locale);
    }

    [Fact]
    public void UseIntl_WithLocaleContext_ReturnsContextAccessor()
    {
        var provider = new InMemoryResourceProvider()
            .Add("fr-FR", "Common", "Hello", "Bonjour");

        var accessor = new IntlAccessor("fr-FR", provider, new MessageCache(), "en-US");
        var localeContext = new LocaleContext(accessor);

        var previous = LocaleContext.Current;
        try
        {
            LocaleContext.Current = localeContext;

            var ctx = new RenderContext();
            ctx.BeginRender(() => { });
            var t = ctx.UseIntl();

            Assert.Equal("fr-FR", t.Locale);
            Assert.Equal("Bonjour", t.Message(new MessageKey("Common", "Hello")));
        }
        finally
        {
            LocaleContext.Current = previous;
        }
    }

    [Fact]
    public void UseIntl_LocaleSwitch_SubscriberNotified()
    {
        var provider = new InMemoryResourceProvider()
            .Add("en-US", "Common", "Hello", "Hello")
            .Add("ar-SA", "Common", "Hello", "مرحبا");

        var cache = new MessageCache();
        var enAccessor = new IntlAccessor("en-US", provider, cache, "en-US");
        var arAccessor = new IntlAccessor("ar-SA", provider, cache, "en-US");
        var localeContext = new LocaleContext(enAccessor);

        var rerenderCount = 0;
        var previous = LocaleContext.Current;
        try
        {
            LocaleContext.Current = localeContext;

            var ctx = new RenderContext();
            ctx.BeginRender(() => rerenderCount++);
            var t = ctx.UseIntl();
            ctx.FlushEffects();

            Assert.Equal("en-US", t.Locale);

            // Simulate locale switch
            localeContext.UpdateAccessor(arAccessor);

            Assert.True(rerenderCount > 0, "Component should have been notified of locale change");
        }
        finally
        {
            LocaleContext.Current = previous;
        }
    }

    [Fact]
    public void LocaleContext_NestedProviders_RestoresPrevious()
    {
        var provider = new InMemoryResourceProvider();
        var cache = new MessageCache();

        var outerAccessor = new IntlAccessor("en-US", provider, cache, "en-US");
        var innerAccessor = new IntlAccessor("fr-FR", provider, cache, "en-US");

        var outerCtx = new LocaleContext(outerAccessor);
        var previous = LocaleContext.Current;

        try
        {
            LocaleContext.Current = outerCtx;
            Assert.Equal("en-US", LocaleContext.Current.Accessor.Locale);

            // Simulate inner provider
            var innerCtx = new LocaleContext(innerAccessor);
            LocaleContext.Current = innerCtx;
            Assert.Equal("fr-FR", LocaleContext.Current.Accessor.Locale);

            // Restore outer
            LocaleContext.Current = outerCtx;
            Assert.Equal("en-US", LocaleContext.Current.Accessor.Locale);
        }
        finally
        {
            LocaleContext.Current = previous;
        }
    }

    [Fact]
    public void Integration_LocaleSwitch_DirectionFlipsAndStringsChange()
    {
        var provider = new InMemoryResourceProvider()
            .Add("en-US", "Common", "Save", "Save")
            .Add("ar-SA", "Common", "Save", "حفظ");

        var cache = new MessageCache();
        var enAccessor = new IntlAccessor("en-US", provider, cache, "en-US");
        var arAccessor = new IntlAccessor("ar-SA", provider, cache, "en-US");

        // Start with English
        Assert.Equal(FlowDirection.LeftToRight, enAccessor.Direction);
        Assert.Equal("Save", enAccessor.Message(new MessageKey("Common", "Save")));

        // Switch to Arabic
        Assert.Equal(FlowDirection.RightToLeft, arAccessor.Direction);
        Assert.Equal("حفظ", arAccessor.Message(new MessageKey("Common", "Save")));
    }
}
