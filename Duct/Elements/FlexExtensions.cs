using Duct.Core;
using Duct.Flex;

namespace Duct;

/// <summary>
/// Extension method for setting flex attached properties on elements.
/// </summary>
public static class FlexExtensions
{
    public static T Flex<T>(this T el,
        double grow = 0,
        double shrink = 1,
        double? basis = null,
        FlexAlign? alignSelf = null,
        FlexPositionType position = FlexPositionType.Relative,
        double? left = null,
        double? top = null,
        double? right = null,
        double? bottom = null
    ) where T : Element =>
        (T)el.SetAttached(new FlexAttached(grow, shrink, basis, alignSelf, position, left, top, right, bottom));
}
