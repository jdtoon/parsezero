using System.Linq.Expressions;
using System.Reflection;

namespace ParseZero.Schema;

/// <summary>
/// Entry point for creating type-safe CSV schemas.
/// </summary>
public static class Schema
{
    /// <summary>
    /// Creates a schema builder for the specified type.
    /// </summary>
    /// <typeparam name="T">The target type to map CSV rows to.</typeparam>
    /// <returns>A schema builder for configuring column mappings.</returns>
    public static SchemaBuilder<T> For<T>() where T : new()
    {
        return new SchemaBuilder<T>();
    }

    /// <summary>
    /// Creates a schema builder for the specified type using a factory function.
    /// </summary>
    /// <typeparam name="T">The target type to map CSV rows to.</typeparam>
    /// <param name="factory">Factory function to create instances.</param>
    /// <returns>A schema builder for configuring column mappings.</returns>
    public static SchemaBuilder<T> For<T>(Func<T> factory)
    {
        return new SchemaBuilder<T>(factory);
    }
}

/// <summary>
/// Fluent builder for defining CSV schema mappings.
/// </summary>
/// <typeparam name="T">The target type to map CSV rows to.</typeparam>
public sealed class SchemaBuilder<T>
{
    private readonly List<ColumnMapping<T>> _columns = new();
    private readonly Func<T>? _factory;
    private bool _hasHeader = true;

    internal SchemaBuilder()
    {
    }

    internal SchemaBuilder(Func<T> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Maps a column to a property.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertySelector">Expression selecting the property.</param>
    /// <param name="format">Optional format string for parsing.</param>
    /// <returns>This builder for chaining.</returns>
    public SchemaBuilder<T> Column<TProperty>(
        Expression<Func<T, TProperty>> propertySelector,
        string? format = null)
    {
        var memberExpression = propertySelector.Body as MemberExpression
            ?? throw new ArgumentException("Expression must be a property access.", nameof(propertySelector));

        var property = memberExpression.Member as PropertyInfo
            ?? throw new ArgumentException("Expression must be a property access.", nameof(propertySelector));

        var mapping = new ColumnMapping<T>
        {
            ColumnIndex = _columns.Count,
            ColumnName = property.Name,
            Property = property,
            Format = format,
            Setter = CreateSetter<TProperty>(property),
            Parser = CreateParser<TProperty>(format)
        };

        _columns.Add(mapping);
        return this;
    }

    /// <summary>
    /// Maps a column by name to a property.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertySelector">Expression selecting the property.</param>
    /// <param name="columnName">The CSV column name.</param>
    /// <param name="format">Optional format string for parsing.</param>
    /// <returns>This builder for chaining.</returns>
    public SchemaBuilder<T> Column<TProperty>(
        Expression<Func<T, TProperty>> propertySelector,
        string columnName,
        string? format = null)
    {
        var memberExpression = propertySelector.Body as MemberExpression
            ?? throw new ArgumentException("Expression must be a property access.", nameof(propertySelector));

        var property = memberExpression.Member as PropertyInfo
            ?? throw new ArgumentException("Expression must be a property access.", nameof(propertySelector));

        var mapping = new ColumnMapping<T>
        {
            ColumnIndex = _columns.Count,
            ColumnName = columnName,
            Property = property,
            Format = format,
            Setter = CreateSetter<TProperty>(property),
            Parser = CreateParser<TProperty>(format)
        };

        _columns.Add(mapping);
        return this;
    }

    /// <summary>
    /// Maps a column by name to a property (alternative parameter order).
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="columnName">The CSV column name.</param>
    /// <param name="propertySelector">Expression selecting the property.</param>
    /// <param name="format">Optional format string for parsing.</param>
    /// <returns>This builder for chaining.</returns>
    public SchemaBuilder<T> Column<TProperty>(
        string columnName,
        Expression<Func<T, TProperty>> propertySelector,
        string? format = null)
    {
        var memberExpression = propertySelector.Body as MemberExpression
            ?? throw new ArgumentException("Expression must be a property access.", nameof(propertySelector));

        var property = memberExpression.Member as PropertyInfo
            ?? throw new ArgumentException("Expression must be a property access.", nameof(propertySelector));

        var mapping = new ColumnMapping<T>
        {
            ColumnIndex = _columns.Count,
            ColumnName = columnName,
            Property = property,
            Format = format,
            Setter = CreateSetter<TProperty>(property),
            Parser = CreateParser<TProperty>(format)
        };

        _columns.Add(mapping);
        return this;
    }

    /// <summary>
    /// Maps a column by index to a property.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="columnIndex">The zero-based column index.</param>
    /// <param name="propertySelector">Expression selecting the property.</param>
    /// <param name="format">Optional format string for parsing.</param>
    /// <returns>This builder for chaining.</returns>
    public SchemaBuilder<T> Column<TProperty>(
        int columnIndex,
        Expression<Func<T, TProperty>> propertySelector,
        string? format = null)
    {
        var memberExpression = propertySelector.Body as MemberExpression
            ?? throw new ArgumentException("Expression must be a property access.", nameof(propertySelector));

        var property = memberExpression.Member as PropertyInfo
            ?? throw new ArgumentException("Expression must be a property access.", nameof(propertySelector));

        var mapping = new ColumnMapping<T>
        {
            ColumnIndex = columnIndex,
            ColumnName = property.Name,
            Property = property,
            Format = format,
            Setter = CreateSetter<TProperty>(property),
            Parser = CreateParser<TProperty>(format)
        };

        _columns.Add(mapping);
        return this;
    }

    /// <summary>
    /// Specifies whether the CSV has a header row.
    /// </summary>
    public SchemaBuilder<T> HasHeader(bool hasHeader = true)
    {
        _hasHeader = hasHeader;
        return this;
    }

    /// <summary>
    /// Builds the schema.
    /// </summary>
    public CsvSchema<T> Build()
    {
        return new CsvSchema<T>(_columns.ToArray(), _factory, _hasHeader);
    }

    private static Action<T, object?> CreateSetter<TProperty>(PropertyInfo property)
    {
        var setMethod = property.GetSetMethod(true)
            ?? throw new InvalidOperationException($"Property {property.Name} does not have a setter.");

        return (obj, value) => setMethod.Invoke(obj, new[] { value });
    }

    private static Func<string, string?, object?> CreateParser<TProperty>(string? format)
    {
        var type = typeof(TProperty);
        var underlyingType = Nullable.GetUnderlyingType(type);
        var isNullable = underlyingType is not null;
        var targetType = underlyingType ?? type;

        return (text, fmt) =>
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return isNullable ? null : GetDefault(targetType);
            }

            return ParseValue(text, targetType, fmt);
        };
    }

    private static object? GetDefault(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }

    private static object? ParseValue(string text, Type type, string? format)
    {
        if (type == typeof(string))
        {
            return text;
        }

        if (type == typeof(int))
        {
            return int.Parse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
        }

        if (type == typeof(long))
        {
            return long.Parse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
        }

        if (type == typeof(double))
        {
            return double.Parse(text, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture);
        }

        if (type == typeof(decimal))
        {
            return decimal.Parse(text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture);
        }

        if (type == typeof(DateTime))
        {
            if (!string.IsNullOrEmpty(format))
            {
                return DateTime.ParseExact(text, format, System.Globalization.CultureInfo.InvariantCulture);
            }
            return DateTime.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        }

        if (type == typeof(DateTimeOffset))
        {
            if (!string.IsNullOrEmpty(format))
            {
                return DateTimeOffset.ParseExact(text, format, System.Globalization.CultureInfo.InvariantCulture);
            }
            return DateTimeOffset.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        }

        if (type == typeof(bool))
        {
            var str = text.ToLowerInvariant();
            return str switch
            {
                "true" or "1" or "yes" or "y" => true,
                "false" or "0" or "no" or "n" => false,
                _ => bool.Parse(str)
            };
        }

        if (type == typeof(Guid))
        {
            return Guid.Parse(text);
        }

        if (type.IsEnum)
        {
            return Enum.Parse(type, text);
        }

        // Fallback: use Convert
        return Convert.ChangeType(text, type, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Implicit conversion to CsvSchema.
    /// </summary>
    public static implicit operator CsvSchema<T>(SchemaBuilder<T> builder) => builder.Build();
}
