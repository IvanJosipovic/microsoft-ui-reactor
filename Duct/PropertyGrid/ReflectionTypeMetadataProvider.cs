using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace Duct.PropertyGrid;

/// <summary>
/// Generates TypeMetadata for CLR types by reflecting over public instance
/// properties and reading attributes. Used as the default fallback by TypeRegistry.
/// </summary>
public static class ReflectionTypeMetadataProvider
{
    private static readonly ConcurrentDictionary<Type, TypeMetadata> _cache = new();

    /// <summary>
    /// Generates TypeMetadata for a CLR type by reflecting over its
    /// public instance properties and reading attributes.
    /// </summary>
    public static TypeMetadata CreateMetadata(Type type)
        => _cache.GetOrAdd(type, BuildMetadata);

    /// <summary>
    /// Generates a PropertyDescriptor for a single PropertyInfo.
    /// </summary>
    public static PropertyDescriptor CreateDescriptor(PropertyInfo property, int defaultOrder)
    {
        // Duct-specific attributes (take precedence)
        var ductCategory = property.GetCustomAttribute<PropertyCategoryAttribute>();
        var ductDescription = property.GetCustomAttribute<PropertyDescriptionAttribute>();
        var ductDisplayName = property.GetCustomAttribute<PropertyDisplayNameAttribute>();
        var ductReadOnly = property.GetCustomAttribute<PropertyReadOnlyAttribute>();
        var ductOrder = property.GetCustomAttribute<PropertyOrderAttribute>();

        // System.ComponentModel fallback
        var scCategory = property.GetCustomAttribute<CategoryAttribute>();
        var scDescription = property.GetCustomAttribute<DescriptionAttribute>();
        var scDisplayName = property.GetCustomAttribute<DisplayNameAttribute>();
        var scReadOnly = property.GetCustomAttribute<ReadOnlyAttribute>();

        string? category = ductCategory?.Name ?? scCategory?.Category;
        string? description = ductDescription?.Text ?? scDescription?.Description;
        string? displayName = ductDisplayName?.Name ?? scDisplayName?.DisplayName;
        int order = ductOrder?.Order ?? defaultOrder;

        bool isReadOnly = ductReadOnly is not null
            || scReadOnly?.IsReadOnly == true
            || !PropertyIsMutable(property);

        var owner = property.DeclaringType!;
        Action<object>? setter = null;
        if (PropertyIsMutable(property))
        {
            setter = val =>
            {
                // SetValue needs the owning object — captured via closure in Decompose
                throw new InvalidOperationException(
                    "SetValue must be created with a captured owner instance. " +
                    "Use the Decompose function which binds to a specific object.");
            };
        }

        return new PropertyDescriptor
        {
            Name = property.Name,
            DisplayName = displayName,
            PropertyType = property.PropertyType,
            GetValue = () => throw new InvalidOperationException(
                "GetValue must be created with a captured owner instance."),
            SetValue = setter,
            Category = category,
            Description = description,
            Order = order,
            IsReadOnly = isReadOnly,
        };
    }

    private static TypeMetadata BuildMetadata(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .Where(p => !IsHidden(p))
            .OrderBy(p =>
            {
                var orderAttr = p.GetCustomAttribute<PropertyOrderAttribute>();
                return orderAttr?.Order ?? p.MetadataToken;
            })
            .ToArray();

        bool hasImmutableProperties = properties.Any(p => !PropertyIsMutable(p));

        Func<object, IReadOnlyList<PropertyDescriptor>> decompose = owner =>
        {
            var result = new List<PropertyDescriptor>();
            for (int i = 0; i < properties.Length; i++)
            {
                var prop = properties[i];
                var desc = CreateDescriptorBound(prop, i, owner);
                result.Add(desc);
            }
            return result;
        };

        Func<object, IReadOnlyDictionary<string, object>, object>? compose = null;
        if (hasImmutableProperties)
            compose = BuildCompose(type, properties);

        return new TypeMetadata
        {
            Decompose = decompose,
            Compose = compose,
        };
    }

    /// <summary>
    /// Creates a PropertyDescriptor with GetValue/SetValue bound to a specific owner instance.
    /// </summary>
    private static PropertyDescriptor CreateDescriptorBound(PropertyInfo property, int defaultOrder, object owner)
    {
        var ductCategory = property.GetCustomAttribute<PropertyCategoryAttribute>();
        var ductDescription = property.GetCustomAttribute<PropertyDescriptionAttribute>();
        var ductDisplayName = property.GetCustomAttribute<PropertyDisplayNameAttribute>();
        var ductReadOnly = property.GetCustomAttribute<PropertyReadOnlyAttribute>();
        var ductOrder = property.GetCustomAttribute<PropertyOrderAttribute>();

        var scCategory = property.GetCustomAttribute<CategoryAttribute>();
        var scDescription = property.GetCustomAttribute<DescriptionAttribute>();
        var scDisplayName = property.GetCustomAttribute<DisplayNameAttribute>();
        var scReadOnly = property.GetCustomAttribute<ReadOnlyAttribute>();

        string? category = ductCategory?.Name ?? scCategory?.Category;
        string? description = ductDescription?.Text ?? scDescription?.Description;
        string? displayName = ductDisplayName?.Name ?? scDisplayName?.DisplayName;
        int order = ductOrder?.Order ?? defaultOrder;

        bool isMutable = PropertyIsMutable(property);
        bool isReadOnly = ductReadOnly is not null
            || scReadOnly?.IsReadOnly == true
            || !isMutable;

        return new PropertyDescriptor
        {
            Name = property.Name,
            DisplayName = displayName,
            PropertyType = property.PropertyType,
            GetValue = () => property.GetValue(owner)!,
            SetValue = isMutable && !isReadOnly
                ? val => property.SetValue(owner, val)
                : null,
            Category = category,
            Description = description,
            Order = order,
            IsReadOnly = isReadOnly,
        };
    }

    private static bool IsHidden(PropertyInfo property)
    {
        if (property.GetCustomAttribute<PropertyHiddenAttribute>() is not null)
            return true;
        var browsable = property.GetCustomAttribute<BrowsableAttribute>();
        if (browsable is not null && !browsable.Browsable)
            return true;
        return false;
    }

    private static bool PropertyIsMutable(PropertyInfo property)
    {
        if (!property.CanWrite) return false;
        var setter = property.SetMethod!;
        // init-only setters have IsInitOnly metadata in newer runtimes,
        // but we detect them via the required custom modifier.
        var retParam = setter.ReturnParameter;
        var requiredMods = retParam.GetRequiredCustomModifiers();
        if (requiredMods.Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit"))
            return false;
        return setter.IsPublic;
    }

    private static Func<object, IReadOnlyDictionary<string, object>, object>? BuildCompose(
        Type type, PropertyInfo[] properties)
    {
        // Try to find a constructor whose parameters match property names (case-insensitive)
        var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        foreach (var ctor in ctors.OrderByDescending(c => c.GetParameters().Length))
        {
            var ctorParams = ctor.GetParameters();
            if (ctorParams.Length == 0) continue;

            // Check if all ctor params match property names
            var paramToProperty = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in properties)
                paramToProperty[prop.Name] = prop;

            bool allMatch = ctorParams.All(p =>
                p.Name is not null && paramToProperty.ContainsKey(p.Name));

            if (!allMatch) continue;

            // Found a matching constructor
            var matchedCtor = ctor;
            var matchedParams = ctorParams;
            var matchedMap = paramToProperty;

            return (currentValue, updates) =>
            {
                var args = new object?[matchedParams.Length];
                for (int i = 0; i < matchedParams.Length; i++)
                {
                    var paramName = matchedParams[i].Name!;
                    if (updates.TryGetValue(paramName, out var updatedValue))
                    {
                        args[i] = updatedValue;
                    }
                    else if (matchedMap.TryGetValue(paramName, out var prop))
                    {
                        args[i] = prop.GetValue(currentValue);
                    }
                }
                return matchedCtor.Invoke(args);
            };
        }

        // Fallback: try Activator.CreateInstance + init-only setter reflection
        var parameterlessCtor = type.GetConstructor(Type.EmptyTypes);
        if (parameterlessCtor is not null)
        {
            return (currentValue, updates) =>
            {
                var newObj = Activator.CreateInstance(type)!;
                foreach (var prop in properties)
                {
                    if (!prop.CanWrite) continue;
                    var value = updates.TryGetValue(prop.Name, out var updated)
                        ? updated
                        : prop.GetValue(currentValue);
                    prop.SetValue(newObj, value);
                }
                return newObj;
            };
        }

        return null;
    }
}
