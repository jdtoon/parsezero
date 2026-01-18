using System.Runtime.CompilerServices;

namespace ParseZero.Simd;

/// <summary>
/// Scalar (non-SIMD) implementation of delimiter scanning.
/// Used as fallback on all platforms and as the only option on .NET Standard 2.0.
/// </summary>
internal sealed class ScalarScanner : IDelimiterScanner
{
    public static readonly ScalarScanner Instance = new();

    private ScalarScanner() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindDelimiter(ReadOnlySpan<char> data, char delimiter)
    {
        return data.IndexOf(delimiter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindLineEnd(ReadOnlySpan<char> data, CsvOptions options)
    {
        if (!options.AllowQuotedFields)
        {
            // Fast path: no quote handling needed
            return FindLineEndFast(data);
        }

        // Slow path: need to track quote state
        return FindLineEndWithQuotes(data, options.Quote);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FindLineEndFast(ReadOnlySpan<char> data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            char c = data[i];
            if (c == '\n' || c == '\r')
            {
                return i;
            }
        }
        return -1;
    }

    private static int FindLineEndWithQuotes(ReadOnlySpan<char> data, char quote)
    {
        bool inQuotes = false;

        for (int i = 0; i < data.Length; i++)
        {
            char c = data[i];

            if (c == quote)
            {
                inQuotes = !inQuotes;
            }
            else if (!inQuotes && (c == '\n' || c == '\r'))
            {
                return i;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int Index, ScanResult Result) FindDelimiterOrLineEnd(ReadOnlySpan<char> data, char delimiter)
    {
        for (int i = 0; i < data.Length; i++)
        {
            char c = data[i];

            if (c == delimiter)
            {
                return (i, ScanResult.Delimiter);
            }

            if (c == '\n')
            {
                return (i, ScanResult.LineFeed);
            }

            if (c == '\r')
            {
                return (i, ScanResult.CarriageReturn);
            }
        }

        return (-1, ScanResult.NotFound);
    }
}
