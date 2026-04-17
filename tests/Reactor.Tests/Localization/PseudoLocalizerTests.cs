using Microsoft.UI.Reactor.Localization;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Localization;

public class PseudoLocalizerTests
{
    [Fact]
    public void Transform_BasicString_AccentsAndWraps()
    {
        var result = PseudoLocalizer.Transform("Hello");
        Assert.StartsWith("[!! ", result);
        Assert.Contains("!!", result);
        Assert.Contains("]", result);
        // Should contain accented characters
        Assert.Contains("é", result); // e -> é
        Assert.Contains("ĺ", result); // l -> ĺ
    }

    [Fact]
    public void Transform_PreservesIcuSyntax()
    {
        var result = PseudoLocalizer.Transform("Hello, {name}!");
        // The {name} should be preserved verbatim
        Assert.Contains("{name}", result);
        // But the text around it should be accented
        Assert.Contains("Ĥ", result); // H -> Ĥ
    }

    [Fact]
    public void Transform_PreservesComplexIcu()
    {
        var input = "{count, plural, one {# item} other {# items}}";
        var result = PseudoLocalizer.Transform(input);
        // ICU tokens preserved (at top level — the regex captures each {...})
        Assert.Contains("{count, plural, one {# item}", result);
    }

    [Fact]
    public void Transform_AddsExpansionPadding()
    {
        var input = "Short";
        var result = PseudoLocalizer.Transform(input);
        // Should be longer than original due to markers + padding
        Assert.True(result.Length > input.Length, $"Expected '{result}' to be longer than '{input}'");
        // Should contain tilde padding
        Assert.Contains("~", result);
    }

    [Fact]
    public void Transform_EmptyString_ReturnsEmpty()
    {
        Assert.Equal("", PseudoLocalizer.Transform(""));
    }

    [Fact]
    public void Transform_NullString_ReturnsNull()
    {
        Assert.Null(PseudoLocalizer.Transform(null!));
    }

    [Fact]
    public void MissingKeyMarker_ReturnsDistinctFormat()
    {
        var key = new MessageKey("Common", "Save");
        var result = PseudoLocalizer.MissingKeyMarker(key);
        Assert.Equal("[?? Common.Save ??]", result);
    }

    [Fact]
    public void IntlAccessor_PseudoMode_WrapsMessages()
    {
        var provider = new InMemoryResourceProvider()
            .Add("en-US", "Common", "Save", "Save");

        var cache = new MessageCache();
        var accessor = new IntlAccessor("en-US", provider, cache, "en-US", pseudoLocalize: true);

        var result = accessor.Message(new MessageKey("Common", "Save"));
        Assert.StartsWith("[!! ", result);
        Assert.Contains("!!", result);
    }

    [Fact]
    public void IntlAccessor_PseudoMode_MissingKeyShowsPseudoMarker()
    {
        var provider = new InMemoryResourceProvider();
        var cache = new MessageCache();
        var accessor = new IntlAccessor("en-US", provider, cache, "en-US", pseudoLocalize: true);

        var result = accessor.Message(new MessageKey("Common", "Missing"));
        Assert.Equal("[?? Common.Missing ??]", result);
    }
}
