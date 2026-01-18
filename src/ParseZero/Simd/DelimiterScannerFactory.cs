using System.Runtime.CompilerServices;
#if NET8_0_OR_GREATER
using System.Runtime.Intrinsics.X86;
#endif

namespace ParseZero.Simd;

/// <summary>
/// Factory for creating the appropriate delimiter scanner based on platform capabilities.
/// </summary>
internal static class DelimiterScannerFactory
{
    /// <summary>
    /// Creates the best available scanner for the current platform.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IDelimiterScanner Create()
    {
#if NET8_0_OR_GREATER
        if (Avx2.IsSupported)
        {
            return Avx2Scanner.Instance;
        }

        if (Sse2.IsSupported)
        {
            return Sse2Scanner.Instance;
        }
#endif
        return ScalarScanner.Instance;
    }

    /// <summary>
    /// Gets information about the available SIMD support.
    /// </summary>
    public static string GetSupportedInstructions()
    {
#if NET8_0_OR_GREATER
        if (Avx2.IsSupported)
        {
            return "AVX2";
        }

        if (Sse2.IsSupported)
        {
            return "SSE2";
        }
#endif
        return "Scalar";
    }
}
