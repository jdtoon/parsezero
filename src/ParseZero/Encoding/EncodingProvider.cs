namespace ParseZero.Encoding;

/// <summary>
/// Provides encoding instances for various character sets.
/// </summary>
public static class EncodingProvider
{
    private static System.Text.Encoding? _iso88591;
    private static System.Text.Encoding? _windows1252;

    /// <summary>
    /// Gets the UTF-8 encoding (without BOM).
    /// </summary>
    public static System.Text.Encoding Utf8 => System.Text.Encoding.UTF8;

    /// <summary>
    /// Gets the UTF-8 encoding with BOM.
    /// </summary>
    public static System.Text.Encoding Utf8Bom => new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    /// <summary>
    /// Gets the UTF-16 Little Endian encoding.
    /// </summary>
    public static System.Text.Encoding Utf16Le => System.Text.Encoding.Unicode;

    /// <summary>
    /// Gets the UTF-16 Big Endian encoding.
    /// </summary>
    public static System.Text.Encoding Utf16Be => System.Text.Encoding.BigEndianUnicode;

    /// <summary>
    /// Gets the ISO-8859-1 (Latin-1) encoding.
    /// </summary>
    public static System.Text.Encoding Iso88591
    {
        get
        {
            if (_iso88591 is null)
            {
                // Register the encoding provider on first access
                RegisterCodePagesEncodingProvider();
                _iso88591 = System.Text.Encoding.GetEncoding("ISO-8859-1");
            }
            return _iso88591;
        }
    }

    /// <summary>
    /// Gets the Windows-1252 (Western European) encoding.
    /// </summary>
    public static System.Text.Encoding Windows1252
    {
        get
        {
            if (_windows1252 is null)
            {
                // Register the encoding provider on first access
                RegisterCodePagesEncodingProvider();
                _windows1252 = System.Text.Encoding.GetEncoding(1252);
            }
            return _windows1252;
        }
    }

    /// <summary>
    /// Gets an encoding by name.
    /// </summary>
    /// <param name="name">The encoding name (e.g., "UTF-8", "ISO-8859-1", "Windows-1252").</param>
    /// <returns>The encoding, or null if not found.</returns>
    public static System.Text.Encoding? GetEncoding(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        name = name.ToUpperInvariant().Replace("-", "").Replace("_", "");

        return name switch
        {
            "UTF8" => Utf8,
            "UTF16" or "UTF16LE" or "UNICODE" => Utf16Le,
            "UTF16BE" => Utf16Be,
            "ISO88591" or "LATIN1" => Iso88591,
            "WINDOWS1252" or "CP1252" => Windows1252,
            "ASCII" => System.Text.Encoding.ASCII,
            _ => TryGetEncodingByName(name)
        };
    }

    /// <summary>
    /// Gets an encoding by code page.
    /// </summary>
    /// <param name="codePage">The code page number.</param>
    /// <returns>The encoding, or null if not found.</returns>
    public static System.Text.Encoding? GetEncoding(int codePage)
    {
        try
        {
            RegisterCodePagesEncodingProvider();
            return System.Text.Encoding.GetEncoding(codePage);
        }
        catch
        {
            return null;
        }
    }

    private static System.Text.Encoding? TryGetEncodingByName(string name)
    {
        try
        {
            RegisterCodePagesEncodingProvider();
            return System.Text.Encoding.GetEncoding(name);
        }
        catch
        {
            return null;
        }
    }

    private static bool _codePagesRegistered;

    private static void RegisterCodePagesEncodingProvider()
    {
        if (_codePagesRegistered)
        {
            return;
        }

        try
        {
            // This is required on .NET Core/.NET 5+ to access legacy code page encodings
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            _codePagesRegistered = true;
        }
        catch
        {
            // Code pages not available (might be trimmed or not supported)
            _codePagesRegistered = true; // Don't retry
        }
    }
}
