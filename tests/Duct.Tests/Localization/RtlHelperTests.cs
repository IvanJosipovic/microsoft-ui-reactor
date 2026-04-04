using Duct.Core.Localization;
using Xunit;

namespace Duct.Tests.Localization;

public class RtlHelperTests
{
    [Theory]
    [InlineData("ar")]
    [InlineData("he")]
    [InlineData("fa")]
    [InlineData("ur")]
    public void IsRtlLocale_KnownRtlLanguages_ReturnsTrue(string locale)
    {
        Assert.True(RtlHelper.IsRtlLocale(locale));
    }

    [Theory]
    [InlineData("ar-SA")]
    [InlineData("he-IL")]
    [InlineData("fa-IR")]
    [InlineData("ur-PK")]
    public void IsRtlLocale_RtlWithRegionSubtag_ReturnsTrue(string locale)
    {
        Assert.True(RtlHelper.IsRtlLocale(locale));
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("ja")]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    [InlineData("ja-JP")]
    public void IsRtlLocale_KnownLtrLocales_ReturnsFalse(string locale)
    {
        Assert.False(RtlHelper.IsRtlLocale(locale));
    }

    [Theory]
    [InlineData("ps")]
    [InlineData("sd")]
    [InlineData("ug")]
    [InlineData("yi")]
    [InlineData("dv")]
    [InlineData("syr")]
    public void IsRtlLocale_LessCommonRtlLanguages_ReturnsTrue(string locale)
    {
        Assert.True(RtlHelper.IsRtlLocale(locale));
    }
}
