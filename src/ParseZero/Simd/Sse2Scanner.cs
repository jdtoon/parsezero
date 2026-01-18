#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ParseZero.Simd;

/// <summary>
/// SSE2-accelerated delimiter scanning.
/// Processes 8 characters (16 bytes) at a time.
/// </summary>
internal sealed class Sse2Scanner : IDelimiterScanner
{
    public static readonly Sse2Scanner Instance = new();

    private Sse2Scanner() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindDelimiter(ReadOnlySpan<char> data, char delimiter)
    {
        if (data.Length < Vector128<ushort>.Count)
        {
            return ScalarScanner.Instance.FindDelimiter(data, delimiter);
        }

        var delimiterVec = Vector128.Create((ushort)delimiter);
        int i = 0;
        int lastVectorStart = data.Length - Vector128<ushort>.Count;

        ref char dataRef = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(data);

        // Process 8 characters at a time
        while (i <= lastVectorStart)
        {
            var chunk = Vector128.LoadUnsafe(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref dataRef, i)));
            var matches = Vector128.Equals(chunk, delimiterVec);

            if (matches != Vector128<ushort>.Zero)
            {
                // Found a match, find the exact position
                uint mask = matches.ExtractMostSignificantBits();
                int offset = System.Numerics.BitOperations.TrailingZeroCount(mask);
                return i + offset;
            }

            i += Vector128<ushort>.Count;
        }

        // Process remaining elements
        for (; i < data.Length; i++)
        {
            if (data[i] == delimiter)
            {
                return i;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindLineEnd(ReadOnlySpan<char> data, CsvOptions options)
    {
        if (options.AllowQuotedFields)
        {
            // Need scalar path for quote handling
            return ScalarScanner.Instance.FindLineEnd(data, options);
        }

        if (data.Length < Vector128<ushort>.Count)
        {
            return ScalarScanner.Instance.FindLineEnd(data, options);
        }

        var lfVec = Vector128.Create((ushort)'\n');
        var crVec = Vector128.Create((ushort)'\r');
        int i = 0;
        int lastVectorStart = data.Length - Vector128<ushort>.Count;

        ref char dataRef = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(data);

        while (i <= lastVectorStart)
        {
            var chunk = Vector128.LoadUnsafe(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref dataRef, i)));

            var lfMatches = Vector128.Equals(chunk, lfVec);
            var crMatches = Vector128.Equals(chunk, crVec);
            var anyMatches = lfMatches | crMatches;

            if (anyMatches != Vector128<ushort>.Zero)
            {
                uint mask = anyMatches.ExtractMostSignificantBits();
                int offset = System.Numerics.BitOperations.TrailingZeroCount(mask);
                return i + offset;
            }

            i += Vector128<ushort>.Count;
        }

        // Process remaining elements
        for (; i < data.Length; i++)
        {
            char c = data[i];
            if (c == '\n' || c == '\r')
            {
                return i;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int Index, ScanResult Result) FindDelimiterOrLineEnd(ReadOnlySpan<char> data, char delimiter)
    {
        if (data.Length < Vector128<ushort>.Count)
        {
            return ScalarScanner.Instance.FindDelimiterOrLineEnd(data, delimiter);
        }

        var delimiterVec = Vector128.Create((ushort)delimiter);
        var lfVec = Vector128.Create((ushort)'\n');
        var crVec = Vector128.Create((ushort)'\r');
        int i = 0;
        int lastVectorStart = data.Length - Vector128<ushort>.Count;

        ref char dataRef = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(data);

        while (i <= lastVectorStart)
        {
            var chunk = Vector128.LoadUnsafe(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref dataRef, i)));

            var delimMatches = Vector128.Equals(chunk, delimiterVec);
            var lfMatches = Vector128.Equals(chunk, lfVec);
            var crMatches = Vector128.Equals(chunk, crVec);
            var anyMatches = delimMatches | lfMatches | crMatches;

            if (anyMatches != Vector128<ushort>.Zero)
            {
                uint mask = anyMatches.ExtractMostSignificantBits();
                int offset = System.Numerics.BitOperations.TrailingZeroCount(mask);
                int idx = i + offset;
                char c = data[idx];

                return c switch
                {
                    '\n' => (idx, ScanResult.LineFeed),
                    '\r' => (idx, ScanResult.CarriageReturn),
                    _ => (idx, ScanResult.Delimiter)
                };
            }

            i += Vector128<ushort>.Count;
        }

        // Process remaining elements
        return ScalarScanner.Instance.FindDelimiterOrLineEnd(data.Slice(i), delimiter) switch
        {
            (-1, _) => (-1, ScanResult.NotFound),
            var (idx, result) => (i + idx, result)
        };
    }
}
#endif
