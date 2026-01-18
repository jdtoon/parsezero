using ParseZero.Core;

namespace ParseZero.Schema;

/// <summary>
/// Represents a compiled CSV schema for mapping rows to objects.
/// </summary>
/// <typeparam name="T">The target type.</typeparam>
public sealed class CsvSchema<T>
{
    private readonly ColumnMapping<T>[] _columns;
    private readonly Func<T>? _factory;
    private readonly bool _hasHeader;
    private Dictionary<string, int>? _headerMap;

    internal CsvSchema(ColumnMapping<T>[] columns, Func<T>? factory, bool hasHeader)
    {
        _columns = columns;
        _factory = factory;
        _hasHeader = hasHeader;
    }

    /// <summary>
    /// Gets whether the CSV has a header row.
    /// </summary>
    public bool HasHeader => _hasHeader;

    /// <summary>
    /// Gets the number of mapped columns.
    /// </summary>
    public int ColumnCount => _columns.Length;

    /// <summary>
    /// Creates an instance of T from a CSV row.
    /// </summary>
    /// <param name="row">The CSV row to map.</param>
    /// <returns>A new instance of T with populated properties.</returns>
    public T Map(CsvRow row)
    {
        T instance = _factory is not null ? _factory() : Activator.CreateInstance<T>();

        foreach (var column in _columns)
        {
            int index = column.ColumnIndex;

            if (index < row.FieldCount)
            {
                var fieldText = row.GetFieldSpan(index).ToString();
                var value = column.Parser(fieldText, column.Format);
                column.Setter(instance, value);
            }
        }

        return instance;
    }

    /// <summary>
    /// Maps the header row to resolve column indices by name.
    /// </summary>
    /// <param name="headerRow">The header row.</param>
    public void MapHeader(CsvRow headerRow)
    {
        _headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < headerRow.FieldCount; i++)
        {
            var name = headerRow.GetFieldSpan(i).ToString().Trim();
            if (!string.IsNullOrEmpty(name))
            {
                _headerMap[name] = i;
            }
        }

        // Update column indices based on header
        foreach (var column in _columns)
        {
            if (_headerMap.TryGetValue(column.ColumnName, out int index))
            {
                column.ColumnIndex = index;
            }
        }
    }

    /// <summary>
    /// Gets the column names in order.
    /// </summary>
    public IReadOnlyList<string> GetColumnNames()
    {
        return _columns.Select(c => c.ColumnName).ToArray();
    }

    /// <summary>
    /// Gets the column types in order.
    /// </summary>
    public IReadOnlyList<Type> GetColumnTypes()
    {
        return _columns.Select(c => c.Property.PropertyType).ToArray();
    }
}

/// <summary>
/// Extension methods for using schemas with CsvReader.
/// </summary>
public static class CsvSchemaExtensions
{
    /// <summary>
    /// Reads a CSV file synchronously and maps rows to objects using a schema.
    /// </summary>
    public static List<T> Read<T>(
        string filePath,
        CsvSchema<T> schema,
        CsvOptions? options = null)
    {
        options ??= CsvOptions.Default;

        var results = new List<T>();

        foreach (var row in CsvReader.Read(filePath, options))
        {
            results.Add(schema.Map(row));
        }

        return results;
    }

    /// <summary>
    /// Reads a CSV stream synchronously and maps rows to objects using a schema.
    /// </summary>
    public static List<T> Read<T>(
        Stream stream,
        CsvSchema<T> schema,
        CsvOptions? options = null)
    {
        options ??= CsvOptions.Default;

        var results = new List<T>();

        foreach (var row in CsvReader.Read(stream, options))
        {
            results.Add(schema.Map(row));
        }

        return results;
    }
}
