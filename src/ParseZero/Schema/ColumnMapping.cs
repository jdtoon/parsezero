using System.Reflection;

namespace ParseZero.Schema;

/// <summary>
/// Represents a mapping between a CSV column and a property.
/// </summary>
/// <typeparam name="T">The target type.</typeparam>
internal sealed class ColumnMapping<T>
{
    /// <summary>
    /// The zero-based column index.
    /// </summary>
    public int ColumnIndex { get; set; }

    /// <summary>
    /// The column name (from header or property name).
    /// </summary>
    public required string ColumnName { get; init; }

    /// <summary>
    /// The target property.
    /// </summary>
    public required PropertyInfo Property { get; init; }

    /// <summary>
    /// Optional format string for parsing.
    /// </summary>
    public string? Format { get; init; }

    /// <summary>
    /// Delegate to set the property value.
    /// </summary>
    public required Action<T, object?> Setter { get; init; }

    /// <summary>
    /// Delegate to parse the value from a string.
    /// </summary>
    public required Func<string, string?, object?> Parser { get; init; }
}
