namespace ParseZero.Simd;

/// <summary>
/// Interface for delimiter and line-end scanning.
/// Different implementations can use SIMD (AVX2/SSE2) or scalar operations.
/// </summary>
internal interface IDelimiterScanner
{
    /// <summary>
    /// Finds the index of a delimiter in the span.
    /// </summary>
    /// <param name="data">The data to search.</param>
    /// <param name="delimiter">The delimiter to find.</param>
    /// <returns>The index of the delimiter, or -1 if not found.</returns>
    int FindDelimiter(ReadOnlySpan<char> data, char delimiter);

    /// <summary>
    /// Finds the index of a line ending (\n or \r) in the span.
    /// </summary>
    /// <param name="data">The data to search.</param>
    /// <param name="options">CSV options for quote handling.</param>
    /// <returns>The index of the line ending, or -1 if not found.</returns>
    int FindLineEnd(ReadOnlySpan<char> data, CsvOptions options);

    /// <summary>
    /// Finds either a delimiter or line ending, whichever comes first.
    /// </summary>
    /// <param name="data">The data to search.</param>
    /// <param name="delimiter">The delimiter to find.</param>
    /// <returns>The index and type of the found character.</returns>
    (int Index, ScanResult Result) FindDelimiterOrLineEnd(ReadOnlySpan<char> data, char delimiter);
}

/// <summary>
/// Result type for scanning operations.
/// </summary>
internal enum ScanResult
{
    /// <summary>
    /// No delimiter or line ending found.
    /// </summary>
    NotFound,

    /// <summary>
    /// Found a field delimiter.
    /// </summary>
    Delimiter,

    /// <summary>
    /// Found a line ending (LF).
    /// </summary>
    LineFeed,

    /// <summary>
    /// Found a carriage return (CR).
    /// </summary>
    CarriageReturn
}
