#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ParseZero.Simd;

/// <summary>
/// AVX2-accelerated delimiter scanning.
/// Processes 16 characters (32 bytes) at a time.
/// </summary>
internal sealed class Avx2Scanner : IDelimiterScanner
{
    public static readonly Avx2Scanner Instance = new();

    private Avx2Scanner() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int FindDelimiter(ReadOnlySpan<char> data, char delimiter)
    {
        if (data.Length < Vector256<ushort>.Count)
        {
            return Sse2Scanner.Instance.FindDelimiter(data, delimiter);
        }

        var delimiterVec = Vector256.Create((ushort)delimiter);
        int i = 0;
        int lastVectorStart = data.Length - Vector256<ushort>.Count;

        ref char dataRef = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(data);

        // Process 16 characters at a time
        while (i <= lastVectorStart)
        {
            var chunk = Vector256.LoadUnsafe(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref dataRef, i)));
            var matches = Vector256.Equals(chunk, delimiterVec);

            if (matches != Vector256<ushort>.Zero)
            {
                // Found a match, find the exact position
                uint mask = matches.ExtractMostSignificantBits();
                int offset = System.Numerics.BitOperations.TrailingZeroCount(mask);
                return i + offset;
            }

            i += Vector256<ushort>.Count;
        }

        // Process remaining with SSE2 or scalar
        if (i < data.Length)
        {
            int result = Sse2Scanner.Instance.FindDelimiter(data.Slice(i), delimiter);
            if (result >= 0)
            {
                return i + result;
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

        if (data.Length < Vector256<ushort>.Count)
        {
            return Sse2Scanner.Instance.FindLineEnd(data, options);
        }

        var lfVec = Vector256.Create((ushort)'\n');
        var crVec = Vector256.Create((ushort)'\r');
        int i = 0;
        int lastVectorStart = data.Length - Vector256<ushort>.Count;

        ref char dataRef = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(data);

        while (i <= lastVectorStart)
        {
            var chunk = Vector256.LoadUnsafe(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref dataRef, i)));

            var lfMatches = Vector256.Equals(chunk, lfVec);
            var crMatches = Vector256.Equals(chunk, crVec);
            var anyMatches = lfMatches | crMatches;

            if (anyMatches != Vector256<ushort>.Zero)
            {
                uint mask = anyMatches.ExtractMostSignificantBits();
                int offset = System.Numerics.BitOperations.TrailingZeroCount(mask);
                return i + offset;
            }

            i += Vector256<ushort>.Count;
        }

        // Process remaining with SSE2 or scalar
        if (i < data.Length)
        {
            int result = Sse2Scanner.Instance.FindLineEnd(data.Slice(i), options);
            if (result >= 0)
            {
                return i + result;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int Index, ScanResult Result) FindDelimiterOrLineEnd(ReadOnlySpan<char> data, char delimiter)
    {
        if (data.Length < Vector256<ushort>.Count)
        {
            return Sse2Scanner.Instance.FindDelimiterOrLineEnd(data, delimiter);
        }

        var delimiterVec = Vector256.Create((ushort)delimiter);
        var lfVec = Vector256.Create((ushort)'\n');
        var crVec = Vector256.Create((ushort)'\r');
        int i = 0;
        int lastVectorStart = data.Length - Vector256<ushort>.Count;

        ref char dataRef = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(data);

        while (i <= lastVectorStart)
        {
            var chunk = Vector256.LoadUnsafe(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref dataRef, i)));

            var delimMatches = Vector256.Equals(chunk, delimiterVec);
            var lfMatches = Vector256.Equals(chunk, lfVec);
            var crMatches = Vector256.Equals(chunk, crVec);
            var anyMatches = delimMatches | lfMatches | crMatches;

            if (anyMatches != Vector256<ushort>.Zero)
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

            i += Vector256<ushort>.Count;
        }

        // Process remaining with SSE2 or scalar
        if (i < data.Length)
        {
            var (idx, result) = Sse2Scanner.Instance.FindDelimiterOrLineEnd(data.Slice(i), delimiter);
            if (idx >= 0)
            {
                return (i + idx, result);
            }
        }

        return (-1, ScanResult.NotFound);
    }
}
#endif
