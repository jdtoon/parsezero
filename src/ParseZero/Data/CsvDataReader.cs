using System.Data;
using System.Globalization;
using ParseZero.Core;

namespace ParseZero.Data;

/// <summary>
/// IDataReader implementation for CSV files.
/// Enables direct integration with SqlBulkCopy.WriteToServer() for high-performance database inserts.
/// </summary>
public sealed class CsvDataReader : IDataReader
{
    private readonly Stream _stream;
    private readonly CsvOptions _options;
    private readonly CsvParser _parser;
    private readonly bool _leaveOpen;

    private string[]? _headers;
    private string[]? _currentValues;
    private int _recordsAffected;
    private bool _closed;
    private bool _disposed;
    private readonly bool _hasHeader;

    /// <summary>
    /// Creates a CsvDataReader from a stream.
    /// </summary>
    public CsvDataReader(Stream stream, CsvOptions? options = null, bool leaveOpen = false)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _options = options ?? CsvOptions.Default;
        _leaveOpen = leaveOpen;
        _hasHeader = _options.HasHeader;

        // Create parser options without HasHeader so we can manually handle it
        var parserOptions = new CsvOptions
        {
            Delimiter = _options.Delimiter,
            Quote = _options.Quote,
            HasHeader = false, // We handle header separately
            Encoding = _options.Encoding,
            BufferSize = _options.BufferSize,
            MaxLineLength = _options.MaxLineLength,
            TrimFields = _options.TrimFields,
            AllowQuotedFields = _options.AllowQuotedFields,
            ExpectedColumnCount = _options.ExpectedColumnCount,
            SkipEmptyLines = _options.SkipEmptyLines,
            CommentCharacter = _options.CommentCharacter
        };
        _parser = new CsvParser(stream, parserOptions);
        _recordsAffected = 0;
        _closed = false;

        // Read header if present
        if (_hasHeader && _parser.TryReadRow(out var headerRow))
        {
            _headers = headerRow.ToStringArray();
        }
    }

    /// <summary>
    /// Creates a CsvDataReader from a file path.
    /// </summary>
    public static CsvDataReader Create(string filePath, CsvOptions? options = null)
    {
        options ??= CsvOptions.Default;

        var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            options.BufferSize,
            FileOptions.SequentialScan);

        return new CsvDataReader(stream, options, leaveOpen: false);
    }

    /// <summary>
    /// Creates a CsvDataReader from a stream.
    /// </summary>
    public static CsvDataReader Create(Stream stream, CsvOptions? options = null, bool leaveOpen = false)
    {
        return new CsvDataReader(stream, options, leaveOpen);
    }

    #region IDataReader Implementation

    /// <inheritdoc />
    public int Depth => 0;

    /// <inheritdoc />
    public bool IsClosed => _closed;

    /// <inheritdoc />
    public int RecordsAffected => _recordsAffected;

    /// <inheritdoc />
    public int FieldCount => _headers?.Length ?? _currentValues?.Length ?? 0;

    /// <inheritdoc />
    public object this[int i] => GetValue(i);

    /// <inheritdoc />
    public object this[string name] => GetValue(GetOrdinal(name));

    /// <inheritdoc />
    public bool Read()
    {
        if (_closed)
        {
            return false;
        }

        if (_parser.TryReadRow(out var currentRow))
        {
            _currentValues = currentRow.ToStringArray();
            _recordsAffected++;
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool NextResult()
    {
        // CSV files don't support multiple result sets
        return false;
    }

    /// <inheritdoc />
    public void Close()
    {
        if (_closed)
        {
            return;
        }

        _closed = true;
        _parser.Dispose();

        if (!_leaveOpen)
        {
            _stream.Dispose();
        }
    }

    /// <inheritdoc />
    public DataTable GetSchemaTable()
    {
        var table = new DataTable("SchemaTable");

        table.Columns.Add("ColumnName", typeof(string));
        table.Columns.Add("ColumnOrdinal", typeof(int));
        table.Columns.Add("ColumnSize", typeof(int));
        table.Columns.Add("DataType", typeof(Type));
        table.Columns.Add("AllowDBNull", typeof(bool));
        table.Columns.Add("IsReadOnly", typeof(bool));
        table.Columns.Add("IsUnique", typeof(bool));
        table.Columns.Add("IsKey", typeof(bool));

        var headers = _headers ?? Enumerable.Range(0, _currentValues?.Length ?? 0)
            .Select(i => $"Column{i}")
            .ToArray();

        for (int i = 0; i < headers.Length; i++)
        {
            var row = table.NewRow();
            row["ColumnName"] = headers[i];
            row["ColumnOrdinal"] = i;
            row["ColumnSize"] = -1; // Unknown
            row["DataType"] = typeof(string);
            row["AllowDBNull"] = true;
            row["IsReadOnly"] = true;
            row["IsUnique"] = false;
            row["IsKey"] = false;
            table.Rows.Add(row);
        }

        return table;
    }

    /// <inheritdoc />
    public string GetName(int i)
    {
        if (_headers is not null && i >= 0 && i < _headers.Length)
        {
            return _headers[i];
        }

        return $"Column{i}";
    }

    /// <inheritdoc />
    public int GetOrdinal(string name)
    {
        if (_headers is null)
        {
            throw new InvalidOperationException("CSV does not have headers.");
        }

        for (int i = 0; i < _headers.Length; i++)
        {
            if (string.Equals(_headers[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new IndexOutOfRangeException($"Column '{name}' not found.");
    }

    /// <inheritdoc />
    public string GetDataTypeName(int i) => "String";

    /// <inheritdoc />
    public Type GetFieldType(int i) => typeof(string);

    /// <inheritdoc />
    public object GetValue(int i)
    {
        if (_currentValues is null)
        {
            throw new InvalidOperationException("No current row. Call Read() first.");
        }

        if (i < 0 || i >= _currentValues.Length)
        {
            throw new IndexOutOfRangeException($"Column index {i} is out of range.");
        }

        var value = _currentValues[i];
        return string.IsNullOrEmpty(value) ? DBNull.Value : value;
    }

    /// <inheritdoc />
    public int GetValues(object[] values)
    {
        if (_currentValues is null)
        {
            throw new InvalidOperationException("No current row. Call Read() first.");
        }

        int count = Math.Min(values.Length, _currentValues.Length);

        for (int i = 0; i < count; i++)
        {
            values[i] = GetValue(i);
        }

        return count;
    }

    /// <inheritdoc />
    public bool IsDBNull(int i)
    {
        if (_currentValues is null)
        {
            throw new InvalidOperationException("No current row. Call Read() first.");
        }

        if (i < 0 || i >= _currentValues.Length)
        {
            throw new IndexOutOfRangeException($"Column index {i} is out of range.");
        }

        return string.IsNullOrEmpty(_currentValues[i]);
    }

    /// <inheritdoc />
    public bool GetBoolean(int i)
    {
        var value = GetString(i);
        return value.ToLowerInvariant() switch
        {
            "true" or "1" or "yes" or "y" => true,
            "false" or "0" or "no" or "n" => false,
            _ => bool.Parse(value)
        };
    }

    /// <inheritdoc />
    public byte GetByte(int i) => byte.Parse(GetString(i), CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length)
    {
        throw new NotSupportedException("GetBytes is not supported for CSV data.");
    }

    /// <inheritdoc />
    public char GetChar(int i)
    {
        var value = GetString(i);
        return value.Length > 0 ? value[0] : '\0';
    }

    /// <inheritdoc />
    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length)
    {
        var value = GetString(i);

        if (buffer is null)
        {
            return value.Length;
        }

        int sourceOffset = (int)fieldoffset;
        int copyLength = Math.Min(length, value.Length - sourceOffset);

        value.CopyTo(sourceOffset, buffer, bufferoffset, copyLength);
        return copyLength;
    }

    /// <inheritdoc />
    public Guid GetGuid(int i) => Guid.Parse(GetString(i));

    /// <inheritdoc />
    public short GetInt16(int i) => short.Parse(GetString(i), CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public int GetInt32(int i) => int.Parse(GetString(i), CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public long GetInt64(int i) => long.Parse(GetString(i), CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public float GetFloat(int i) => float.Parse(GetString(i), CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public double GetDouble(int i) => double.Parse(GetString(i), CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public decimal GetDecimal(int i) => decimal.Parse(GetString(i), CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public DateTime GetDateTime(int i) => DateTime.Parse(GetString(i), CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public string GetString(int i)
    {
        if (_currentValues is null)
        {
            throw new InvalidOperationException("No current row. Call Read() first.");
        }

        if (i < 0 || i >= _currentValues.Length)
        {
            throw new IndexOutOfRangeException($"Column index {i} is out of range.");
        }

        return _currentValues[i];
    }

    /// <inheritdoc />
    public IDataReader GetData(int i)
    {
        throw new NotSupportedException("Nested data readers are not supported.");
    }

    #endregion

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Close();
    }

    #endregion
}
