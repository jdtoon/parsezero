using System.Buffers.Text;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace ParseZero.Core;

/// <summary>
/// Represents a single CSV field with zero-allocation parsing methods.
/// This is a ref struct that cannot escape the stack.
/// </summary>
public readonly ref struct CsvField
{
    private readonly ReadOnlySpan<char> _value;
    private readonly CsvOptions _options;

    internal CsvField(ReadOnlySpan<char> value, CsvOptions options)
    {
        _value = value;
        _options = options;
    }

    /// <summary>
    /// Gets the raw span of the field value.
    /// </summary>
    public ReadOnlySpan<char> Span => _value;

    /// <summary>
    /// Gets the length of the field value.
    /// </summary>
    public int Length => _value.Length;

    /// <summary>
    /// Gets whether the field is empty.
    /// </summary>
    public bool IsEmpty => _value.IsEmpty;

    /// <summary>
    /// Gets whether the field is null or whitespace.
    /// </summary>
    public bool IsNullOrWhiteSpace => _value.IsEmpty || _value.IsWhiteSpace();

    #region Parse Methods (Zero Allocation)

    /// <summary>
    /// Parses the field as an Int32.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ParseInt32()
    {
#if NET8_0_OR_GREATER
        return int.Parse(_value, NumberStyles.Integer, CultureInfo.InvariantCulture);
#else
        return int.Parse(_value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture);
#endif
    }

    /// <summary>
    /// Tries to parse the field as an Int32.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryParseInt32(out int value)
    {
#if NET8_0_OR_GREATER
        return int.TryParse(_value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
#else
        return int.TryParse(_value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
#endif
    }

    /// <summary>
    /// Parses the field as an Int64.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ParseInt64()
    {
#if NET8_0_OR_GREATER
        return long.Parse(_value, NumberStyles.Integer, CultureInfo.InvariantCulture);
#else
        return long.Parse(_value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture);
#endif
    }

    /// <summary>
    /// Tries to parse the field as an Int64.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryParseInt64(out long value)
    {
#if NET8_0_OR_GREATER
        return long.TryParse(_value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
#else
        return long.TryParse(_value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
#endif
    }

    /// <summary>
    /// Parses the field as a Double.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ParseDouble()
    {
#if NET8_0_OR_GREATER
        return double.Parse(_value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
#else
        return double.Parse(_value.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
#endif
    }

    /// <summary>
    /// Tries to parse the field as a Double.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryParseDouble(out double value)
    {
#if NET8_0_OR_GREATER
        return double.TryParse(_value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
#else
        return double.TryParse(_value.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
#endif
    }

    /// <summary>
    /// Parses the field as a Decimal.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal ParseDecimal()
    {
#if NET8_0_OR_GREATER
        return decimal.Parse(_value, NumberStyles.Number, CultureInfo.InvariantCulture);
#else
        return decimal.Parse(_value.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture);
#endif
    }

    /// <summary>
    /// Tries to parse the field as a Decimal.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryParseDecimal(out decimal value)
    {
#if NET8_0_OR_GREATER
        return decimal.TryParse(_value, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
#else
        return decimal.TryParse(_value.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
#endif
    }

    /// <summary>
    /// Parses the field as a DateTime.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTime ParseDateTime()
    {
#if NET8_0_OR_GREATER
        return DateTime.Parse(_value, CultureInfo.InvariantCulture, DateTimeStyles.None);
#else
        return DateTime.Parse(_value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None);
#endif
    }

    /// <summary>
    /// Parses the field as a DateTime with a specific format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTime ParseDateTimeExact(string format)
    {
#if NET8_0_OR_GREATER
        return DateTime.ParseExact(_value, format, CultureInfo.InvariantCulture, DateTimeStyles.None);
#else
        return DateTime.ParseExact(_value.ToString(), format, CultureInfo.InvariantCulture, DateTimeStyles.None);
#endif
    }

    /// <summary>
    /// Tries to parse the field as a DateTime.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryParseDateTime(out DateTime value)
    {
#if NET8_0_OR_GREATER
        return DateTime.TryParse(_value, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
#else
        return DateTime.TryParse(_value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
#endif
    }

    /// <summary>
    /// Tries to parse the field as a DateTime with a specific format.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryParseDateTimeExact(string format, out DateTime value)
    {
#if NET8_0_OR_GREATER
        return DateTime.TryParseExact(_value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
#else
        return DateTime.TryParseExact(_value.ToString(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
#endif
    }

    /// <summary>
    /// Parses the field as a Boolean.
    /// Accepts: true/false, 1/0, yes/no, y/n (case-insensitive)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ParseBoolean()
    {
        if (TryParseBoolean(out bool value))
        {
            return value;
        }

        throw new FormatException($"Cannot parse '{_value.ToString()}' as Boolean.");
    }

    /// <summary>
    /// Tries to parse the field as a Boolean.
    /// </summary>
    public bool TryParseBoolean(out bool value)
    {
        var span = _value;

        // Handle empty
        if (span.IsEmpty)
        {
            value = false;
            return false;
        }

        // Handle numeric
        if (span.Length == 1)
        {
            if (span[0] == '1')
            {
                value = true;
                return true;
            }
            if (span[0] == '0')
            {
                value = false;
                return true;
            }
        }

        // Handle text values
        if (span.Equals("true".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
            span.Equals("yes".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
            span.Equals("y".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        if (span.Equals("false".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
            span.Equals("no".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
            span.Equals("n".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        value = false;
        return false;
    }

    /// <summary>
    /// Parses the field as a Guid.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Guid ParseGuid()
    {
#if NET8_0_OR_GREATER
        return Guid.Parse(_value);
#else
        return Guid.Parse(_value.ToString());
#endif
    }

    /// <summary>
    /// Tries to parse the field as a Guid.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryParseGuid(out Guid value)
    {
#if NET8_0_OR_GREATER
        return Guid.TryParse(_value, out value);
#else
        return Guid.TryParse(_value.ToString(), out value);
#endif
    }

    #endregion

    #region Comparison Methods

    /// <summary>
    /// Compares the field value to a string.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(string? other)
    {
        if (other is null)
        {
            return _value.IsEmpty;
        }
        return _value.Equals(other.AsSpan(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Compares the field value to a string with specified comparison.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(string? other, StringComparison comparison)
    {
        if (other is null)
        {
            return _value.IsEmpty;
        }
        return _value.Equals(other.AsSpan(), comparison);
    }

    /// <summary>
    /// Compares the field value to a span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ReadOnlySpan<char> other)
    {
        return _value.Equals(other, StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks if the field starts with the specified value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(string value, StringComparison comparison = StringComparison.Ordinal)
    {
        return _value.StartsWith(value.AsSpan(), comparison);
    }

    /// <summary>
    /// Checks if the field ends with the specified value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EndsWith(string value, StringComparison comparison = StringComparison.Ordinal)
    {
        return _value.EndsWith(value.AsSpan(), comparison);
    }

    /// <summary>
    /// Checks if the field contains the specified value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(string value, StringComparison comparison = StringComparison.Ordinal)
    {
#if NET8_0_OR_GREATER
        return _value.Contains(value.AsSpan(), comparison);
#else
        return _value.ToString().Contains(value);
#endif
    }

    #endregion

    /// <summary>
    /// Converts the field to a string. This allocates memory.
    /// For escaped quotes (""), this method properly unescapes them.
    /// </summary>
    public override string ToString()
    {
        if (_value.IsEmpty)
        {
            return string.Empty;
        }

        // Check if we need to unescape quotes
        if (_options.AllowQuotedFields)
        {
            int quoteIndex = _value.IndexOf(_options.Quote);
            if (quoteIndex >= 0 && quoteIndex < _value.Length - 1 &&
                _value[quoteIndex + 1] == _options.Quote)
            {
                // Has escaped quotes, need to unescape
                return UnescapeQuotes();
            }
        }

        return _value.ToString();
    }

    private string UnescapeQuotes()
    {
        var sb = new StringBuilder(_value.Length);
        char quote = _options.Quote;

        for (int i = 0; i < _value.Length; i++)
        {
            char c = _value[i];
            if (c == quote && i + 1 < _value.Length && _value[i + 1] == quote)
            {
                sb.Append(quote);
                i++; // Skip the next quote
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Copies the field value to a destination span.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <returns>The number of characters written.</returns>
    public int CopyTo(Span<char> destination)
    {
        if (destination.Length < _value.Length)
        {
            throw new ArgumentException("Destination span is too small.", nameof(destination));
        }

        _value.CopyTo(destination);
        return _value.Length;
    }
}
