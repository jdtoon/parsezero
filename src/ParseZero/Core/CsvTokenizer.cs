using System.Buffers;
using System.Runtime.CompilerServices;

namespace ParseZero.Core;

/// <summary>
/// Low-level CSV tokenizer that operates on spans.
/// This is the foundation for all parsing operations.
/// </summary>
internal static class CsvTokenizer
{
    /// <summary>
    /// Finds the end of a field in the given span.
    /// </summary>
    /// <param name="data">The data to search.</param>
    /// <param name="delimiter">The field delimiter.</param>
    /// <param name="quote">The quote character.</param>
    /// <param name="allowQuotes">Whether quoted fields are allowed.</param>
    /// <returns>The index of the delimiter or end of data.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FindFieldEnd(ReadOnlySpan<char> data, char delimiter, char quote, bool allowQuotes)
    {
        if (data.IsEmpty)
        {
            return 0;
        }

        bool inQuotes = false;
        bool wasQuote = false;

        for (int i = 0; i < data.Length; i++)
        {
            char c = data[i];

            if (allowQuotes)
            {
                if (c == quote)
                {
                    if (wasQuote)
                    {
                        // Escaped quote
                        wasQuote = false;
                        continue;
                    }

                    if (inQuotes)
                    {
                        // Could be end of quoted field or escaped quote
                        wasQuote = true;
                        continue;
                    }
                    else if (i == 0 || data[i - 1] == delimiter)
                    {
                        // Start of quoted field
                        inQuotes = true;
                        continue;
                    }
                }

                if (wasQuote)
                {
                    // Previous quote was end of quoted field
                    inQuotes = false;
                    wasQuote = false;

                    if (c == delimiter)
                    {
                        return i;
                    }
                }

                if (!inQuotes && c == delimiter)
                {
                    return i;
                }
            }
            else
            {
                if (c == delimiter)
                {
                    return i;
                }
            }
        }

        return data.Length;
    }

    /// <summary>
    /// Finds the end of a line in the given span.
    /// </summary>
    /// <param name="data">The data to search.</param>
    /// <param name="quote">The quote character.</param>
    /// <param name="allowQuotes">Whether quoted fields are allowed.</param>
    /// <returns>The index of the line ending, or -1 if not found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FindLineEnd(ReadOnlySpan<char> data, char quote, bool allowQuotes)
    {
        if (data.IsEmpty)
        {
            return -1;
        }

        bool inQuotes = false;

        for (int i = 0; i < data.Length; i++)
        {
            char c = data[i];

            if (allowQuotes && c == quote)
            {
                // Toggle quote state
                // Note: For escaped quotes (""), this will toggle twice, ending up in the same state
                // This is correct for finding line endings
                inQuotes = !inQuotes;
            }
            else if (!inQuotes && (c == '\n' || c == '\r'))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Counts the number of fields in a line.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountFields(ReadOnlySpan<char> line, char delimiter, char quote, bool allowQuotes)
    {
        if (line.IsEmpty)
        {
            return 0;
        }

        int count = 1;
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (allowQuotes && c == quote)
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

    /// <summary>
    /// Extracts a field from a line at the specified index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetField(ReadOnlySpan<char> line, int index, char delimiter, char quote, bool allowQuotes)
    {
        if (line.IsEmpty)
        {
            return index == 0 ? ReadOnlySpan<char>.Empty : throw new ArgumentOutOfRangeException(nameof(index));
        }

        int currentField = 0;
        int fieldStart = 0;
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (allowQuotes && c == quote)
            {
                inQuotes = !inQuotes;
            }
            else if (!inQuotes && c == delimiter)
            {
                if (currentField == index)
                {
                    return line.Slice(fieldStart, i - fieldStart);
                }

                currentField++;
                fieldStart = i + 1;
            }
        }

        // Last field
        if (currentField == index)
        {
            return line.Slice(fieldStart);
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <summary>
    /// Removes surrounding quotes and unescapes doubled quotes.
    /// </summary>
    public static ReadOnlySpan<char> UnquoteField(ReadOnlySpan<char> field, char quote)
    {
        if (field.Length < 2)
        {
            return field;
        }

        if (field[0] == quote && field[field.Length - 1] == quote)
        {
            return field.Slice(1, field.Length - 2);
        }

        return field;
    }

    /// <summary>
    /// Checks if a field contains escaped quotes that need unescaping.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasEscapedQuotes(ReadOnlySpan<char> field, char quote)
    {
        int idx = field.IndexOf(quote);
        return idx >= 0 && idx < field.Length - 1 && field[idx + 1] == quote;
    }

    /// <summary>
    /// Gets the number of bytes needed to store the unescaped field.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetUnescapedLength(ReadOnlySpan<char> field, char quote)
    {
        int length = field.Length;
        int quoteCount = 0;

        for (int i = 0; i < field.Length - 1; i++)
        {
            if (field[i] == quote && field[i + 1] == quote)
            {
                quoteCount++;
                i++; // Skip the second quote
            }
        }

        return length - quoteCount;
    }
}
