using Duct.Core;
using static Duct.UI;

namespace Duct.PropertyGrid;

/// <summary>
/// Maps CLR types to TypeMetadata for the PropertyGrid.
/// Provides a fluent Register API and a Resolve method with built-in fallbacks.
/// </summary>
public class TypeRegistry
{
    private readonly Dictionary<Type, TypeMetadata> _map = new();

    /// <summary>Register metadata for a type.</summary>
    public TypeRegistry Register<T>(TypeMetadata metadata)
    {
        _map[typeof(T)] = metadata;
        return this;
    }

    /// <summary>
    /// Resolve metadata for a type. Falls back to built-in rules:
    /// 1. Exact match in registry
    /// 2. Enum — auto-generated ComboBox editor
    /// 3. CLR primitive — built-in editor
    /// 4. Array/IList&lt;T&gt; — array editor
    /// 5. Record/class/struct — reflection-based decomposition
    /// </summary>
    public TypeMetadata Resolve(Type type)
    {
        // 1. Exact match
        if (_map.TryGetValue(type, out var metadata))
            return metadata;

        // 2. Enum
        if (type.IsEnum)
            return ResolveEnum(type);

        // 3. CLR primitives
        if (TryResolvePrimitive(type, out var primitive))
            return primitive;

        // 4. Array / IList<T>
        if (TryResolveArray(type, out var array))
            return array;

        // 5. Reflection fallback
        return ReflectionTypeMetadataProvider.CreateMetadata(type);
    }

    private static TypeMetadata ResolveEnum(Type enumType)
    {
        var names = Enum.GetNames(enumType);
        return new TypeMetadata
        {
            Editor = (value, onChange) =>
            {
                var currentIndex = Array.IndexOf(names, value.ToString());
                return ComboBox(names, currentIndex >= 0 ? currentIndex : 0,
                    index => onChange(Enum.Parse(enumType, names[index])));
            }
        };
    }

    private static bool TryResolvePrimitive(Type type, out TypeMetadata metadata)
    {
        metadata = null!;

        if (type == typeof(string))
        {
            metadata = new TypeMetadata
            {
                Editor = (value, onChange) =>
                    TextField((string)(value ?? ""), s => onChange(s))
            };
            return true;
        }

        if (type == typeof(bool))
        {
            metadata = new TypeMetadata
            {
                Editor = (value, onChange) =>
                    ToggleSwitch((bool)value, v => onChange(v))
            };
            return true;
        }

        if (type == typeof(int))
        {
            metadata = new TypeMetadata
            {
                Editor = (value, onChange) =>
                    NumberBox((int)value, v => onChange((int)v))
            };
            return true;
        }

        if (type == typeof(long))
        {
            metadata = new TypeMetadata
            {
                Editor = (value, onChange) =>
                    NumberBox((long)value, v => onChange((long)v))
            };
            return true;
        }

        if (type == typeof(short))
        {
            metadata = new TypeMetadata
            {
                Editor = (value, onChange) =>
                    NumberBox((short)value, v => onChange((short)v))
            };
            return true;
        }

        if (type == typeof(byte))
        {
            metadata = new TypeMetadata
            {
                Editor = (value, onChange) =>
                    NumberBox((byte)value, v => onChange((byte)v))
            };
            return true;
        }

        if (type == typeof(float))
        {
            metadata = new TypeMetadata
            {
                Editor = (value, onChange) =>
                    NumberBox((float)value, v => onChange((float)v))
            };
            return true;
        }

        if (type == typeof(double))
        {
            metadata = new TypeMetadata
            {
                Editor = (value, onChange) =>
                    NumberBox((double)value, v => onChange(v))
            };
            return true;
        }

        if (type == typeof(decimal))
        {
            metadata = new TypeMetadata
            {
                Editor = (value, onChange) =>
                    NumberBox((double)(decimal)value, v => onChange((decimal)v))
            };
            return true;
        }

        return false;
    }

    private static bool TryResolveArray(Type type, out TypeMetadata metadata)
    {
        metadata = null!;
        Type? elementType = null;

        if (type.IsArray)
        {
            elementType = type.GetElementType();
        }
        else if (type.IsGenericType)
        {
            var genDef = type.GetGenericTypeDefinition();
            if (genDef == typeof(List<>) || genDef == typeof(IList<>))
                elementType = type.GetGenericArguments()[0];
        }

        if (elementType is null)
            return false;

        Func<Task<object?>>? createElement = null;
        if (elementType.GetConstructor(Type.EmptyTypes) is not null)
        {
            createElement = () => Task.FromResult<object?>(Activator.CreateInstance(elementType));
        }

        metadata = new ArrayTypeMetadata
        {
            CreateElement = createElement,
        };
        return true;
    }
}
