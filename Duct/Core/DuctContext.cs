using System.Runtime.CompilerServices;

namespace Duct.Core;

/// <summary>
/// Non-generic base for type-erased storage in the context scope stack.
/// </summary>
public abstract class DuctContextBase
{
    internal abstract object? DefaultValueBoxed { get; }
}

/// <summary>
/// A typed, named context that can be provided to a subtree and consumed by any descendant.
/// Define as a static field. Provide via .Provide() modifier. Consume via UseContext() hook.
/// </summary>
public sealed class DuctContext<T> : DuctContextBase
{
    public T DefaultValue { get; }
    internal string? DebugName { get; }

    public DuctContext(T defaultValue, [CallerMemberName] string? name = null)
    {
        DefaultValue = defaultValue;
        DebugName = name;
    }

    internal override object? DefaultValueBoxed => DefaultValue;
}
