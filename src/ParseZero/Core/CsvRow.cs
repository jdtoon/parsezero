using System.Buffers;
using System.Runtime.CompilerServices;

namespace ParseZero.Core;

/// <summary>
/// Represents a single CSV row with zero-allocation field access.
/// This is a ref struct that cannot escape the stack.
/// </summary>
public readonly ref struct CsvRow
{
    private readonly ReadOnlySpan<char> _line;
    private readonly CsvOptions _options;
    private readonly int _rowNumber;

    internal CsvRow(ReadOnlySpan<char> line, CsvOptions options, int rowNumber)
    {
        _line = line;
        _options = options;
        _rowNumber = rowNumber;
    }

    /// <summary>
    /// Gets the 1-based row number in the source file.
    /// </summary>
    public int RowNumber => _rowNumber;

    /// <summary>
    /// Gets the raw line content.
    /// </summary>
    public ReadOnlySpan<char> RawLine => _line;

    /// <summary>
    /// Gets the number of fields in this row.
    /// </summary>
    public int FieldCount
    {
        get
        {
            if (_line.IsEmpty)
            {
                return 0;
            }

            int count = 1;
            bool inQuotes = false;
            char delimiter = _options.Delimiter;
            char quote = _options.Quote;

            for (int i = 0; i < _line.Length; i++)
            {
                char c = _line[i];

                if (_options.AllowQuotedFields && c == quote)
                {
                    inQuotes = !inQuotes;
                }
                else if (!inQuotes && c == delimiter)
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>
    /// Gets a field by index.
    /// </summary>
    /// <param name="index">Zero-based field index.</param>
    /// <returns>The field value as a CsvField.</returns>
    public CsvField this[int index] => GetField(index);

    /// <summary>
    /// Gets a field by index.
    /// </summary>
    /// <param name="index">Zero-based field index.</param>
    /// <returns>The field value as a CsvField.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CsvField GetField(int index)
    {
        if (index < 0)
        {
            ThrowArgumentOutOfRange(index);
        }

        var span = GetFieldSpan(index);
        return new CsvField(span, _options);
    }

    /// <summary>
    /// Gets a field's raw span by index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> GetFieldSpan(int index)
    {
        if (_line.IsEmpty)
        {
            if (index == 0)
            {
                return ReadOnlySpan<char>.Empty;
            }
            ThrowArgumentOutOfRange(index);
        }

        int currentField = 0;
        int fieldStart = 0;
        bool inQuotes = false;
        char delimiter = _options.Delimiter;
        char quote = _options.Quote;

        for (int i = 0; i < _line.Length; i++)
        {
            char c = _line[i];

            if (_options.AllowQuotedFields && c == quote)
            {
                inQuotes = !inQuotes;
            }
            else if (!inQuotes && c == delimiter)
            {
                if (currentField == index)
                {
                    return ExtractField(_line.Slice(fieldStart, i - fieldStart));
                }
                currentField++;
                fieldStart = i + 1;
            }
        }

        // Last field (or only field)
        if (currentField == index)
        {
            return ExtractField(_line.Slice(fieldStart));
        }

        ThrowArgumentOutOfRange(index);
        return default; // Unreachable
    }

    /// <summary>
    /// Extracts a field, handling quotes and trimming if needed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<char> ExtractField(ReadOnlySpan<char> field)
    {
        if (_options.TrimFields)
        {
            field = field.Trim();
        }

        // Handle quoted fields
        if (_options.AllowQuotedFields && field.Length >= 2 &&
            field[0] == _options.Quote && field[field.Length - 1] == _options.Quote)
        {
            field = field.Slice(1, field.Length - 2);

            // Note: Escaped quotes ("") within the field are NOT unescaped here
            // to avoid allocation. Use CsvField.ToString() for unescaped value.
        }

        return field;
    }

    /// <summary>
    /// Enumerates all fields in the row.
    /// </summary>
    public FieldEnumerator GetEnumerator() => new(this);

    /// <summary>
    /// Copies all field values to a string array.
    /// This allocates memory and should be avoided in hot paths.
    /// </summary>
    public string[] ToStringArray()
    {
        int count = FieldCount;
        var result = new string[count];

        for (int i = 0; i < count; i++)
        {
            result[i] = GetField(i).ToString();
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowArgumentOutOfRange(int index)
    {
        throw new ArgumentOutOfRangeException(nameof(index), index, "Field index is out of range.");
    }

    /// <summary>
    /// Enumerator for fields in a CSV row.
    /// </summary>
    public ref struct FieldEnumerator
    {
        private readonly CsvRow _row;
        private int _index;
        private CsvField _current;

        internal FieldEnumerator(CsvRow row)
        {
            _row = row;
            _index = -1;
            _current = default;
        }

        /// <summary>
        /// Gets the current field.
        /// </summary>
        public readonly CsvField Current => _current;

        /// <summary>
        /// Advances to the next field.
        /// </summary>
        public bool MoveNext()
        {
            _index++;
            if (_index < _row.FieldCount)
            {
                _current = _row.GetField(_index);
                return true;
            }
            return false;
        }
    }
}
