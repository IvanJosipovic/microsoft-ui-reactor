using Microsoft.UI.Reactor.Localization.Generator;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Localization;

public class ReswParserTests
{
    private const string SampleResw = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <data name=""Save"" xml:space=""preserve"">
    <value>Save</value>
  </data>
  <data name=""Cancel"" xml:space=""preserve"">
    <value>Cancel</value>
  </data>
  <data name=""ItemCount"" xml:space=""preserve"">
    <value>{count, plural, one {# item} other {# items}}</value>
    <comment>ai-translated: pending-review</comment>
  </data>
</root>";

    [Fact]
    public void Parse_ReturnsAllEntries()
    {
        var entries = ReswParser.Parse(SampleResw);
        Assert.Equal(3, entries.Count);
    }

    [Fact]
    public void Parse_CorrectKeyAndValue()
    {
        var entries = ReswParser.Parse(SampleResw);
        Assert.Equal("Save", entries[0].Key);
        Assert.Equal("Save", entries[0].Value);
        Assert.Equal("Cancel", entries[1].Key);
        Assert.Equal("Cancel", entries[1].Value);
    }

    [Fact]
    public void Parse_PreservesIcuValues()
    {
        var entries = ReswParser.Parse(SampleResw);
        var icu = entries.Find(e => e.Key == "ItemCount");
        Assert.NotNull(icu);
        Assert.Equal("{count, plural, one {# item} other {# items}}", icu!.Value);
    }

    [Fact]
    public void Parse_ReadsComments()
    {
        var entries = ReswParser.Parse(SampleResw);
        var icu = entries.Find(e => e.Key == "ItemCount");
        Assert.NotNull(icu);
        Assert.Equal("ai-translated: pending-review", icu!.Comment);
    }

    [Fact]
    public void Parse_NullCommentWhenAbsent()
    {
        var entries = ReswParser.Parse(SampleResw);
        Assert.Null(entries[0].Comment);
    }

    [Fact]
    public void Parse_EmptyFile_ReturnsEmptyList()
    {
        var entries = ReswParser.Parse(@"<?xml version=""1.0"" encoding=""utf-8""?><root></root>");
        Assert.Empty(entries);
    }

    [Fact]
    public void IsFlatLayout_SingleResources_ReturnsTrue()
    {
        Assert.True(ReswParser.IsFlatLayout(new[] { "Resources" }));
    }

    [Fact]
    public void IsFlatLayout_MultipleFiles_ReturnsFalse()
    {
        Assert.False(ReswParser.IsFlatLayout(new[] { "Common", "Settings" }));
    }

    [Fact]
    public void IsFlatLayout_SingleNonResources_ReturnsFalse()
    {
        Assert.False(ReswParser.IsFlatLayout(new[] { "Common" }));
    }
}
