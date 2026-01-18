namespace ParseZero.Encoding;

/// <summary>
/// Detects encoding from Byte Order Marks (BOM) at the beginning of a stream.
/// </summary>
internal static class BomDetector
{
    // BOM signatures
    private static readonly byte[] Utf8Bom = { 0xEF, 0xBB, 0xBF };
    private static readonly byte[] Utf16LeBom = { 0xFF, 0xFE };
    private static readonly byte[] Utf16BeBom = { 0xFE, 0xFF };
    private static readonly byte[] Utf32LeBom = { 0xFF, 0xFE, 0x00, 0x00 };
    private static readonly byte[] Utf32BeBom = { 0x00, 0x00, 0xFE, 0xFF };

    /// <summary>
    /// Detects the encoding from the BOM at the current position of the stream.
    /// If a BOM is found, the stream is positioned after it.
    /// If no BOM is found, the stream position is unchanged.
    /// </summary>
    /// <param name="stream">The stream to check.</param>
    /// <returns>The detected encoding, or null if no BOM was found.</returns>
    public static System.Text.Encoding? DetectEncoding(Stream stream)
    {
        if (!stream.CanSeek)
        {
            // Can't detect BOM on non-seekable streams
            return null;
        }

        long startPosition = stream.Position;

        // Read enough bytes to detect any BOM (max 4 bytes)
        byte[] buffer = new byte[4];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);

        if (bytesRead < 2)
        {
            // Not enough data for any BOM
            stream.Position = startPosition;
            return null;
        }

        // Check for UTF-32 first (it has a longer signature that overlaps with UTF-16 LE)
        if (bytesRead >= 4)
        {
            if (StartsWith(buffer, Utf32LeBom))
            {
                stream.Position = startPosition + 4;
                return System.Text.Encoding.UTF32;
            }

            if (StartsWith(buffer, Utf32BeBom))
            {
                stream.Position = startPosition + 4;
                return new System.Text.UTF32Encoding(bigEndian: true, byteOrderMark: true);
            }
        }

        // Check for UTF-8 BOM (3 bytes)
        if (bytesRead >= 3 && StartsWith(buffer, Utf8Bom))
        {
            stream.Position = startPosition + 3;
            return System.Text.Encoding.UTF8;
        }

        // Check for UTF-16 BOMs (2 bytes)
        if (StartsWith(buffer, Utf16LeBom))
        {
            stream.Position = startPosition + 2;
            return System.Text.Encoding.Unicode; // UTF-16 LE
        }

        if (StartsWith(buffer, Utf16BeBom))
        {
            stream.Position = startPosition + 2;
            return System.Text.Encoding.BigEndianUnicode; // UTF-16 BE
        }

        // No BOM found, reset position
        stream.Position = startPosition;
        return null;
    }

    private static bool StartsWith(byte[] buffer, byte[] prefix)
    {
        if (buffer.Length < prefix.Length) return false;
        for (int i = 0; i < prefix.Length; i++)
        {
            if (buffer[i] != prefix[i]) return false;
        }
        return true;
    }

    /// <summary>
    /// Detects the encoding from a byte array BOM.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <param name="bomLength">The length of the BOM if found.</param>
    /// <returns>The detected encoding, or null if no BOM was found.</returns>
    public static System.Text.Encoding? DetectEncoding(ReadOnlySpan<byte> data, out int bomLength)
    {
        bomLength = 0;

        if (data.Length < 2)
        {
            return null;
        }

        // Check for UTF-32 first
        if (data.Length >= 4)
        {
            if (data.Slice(0, 4).SequenceEqual(Utf32LeBom))
            {
                bomLength = 4;
                return System.Text.Encoding.UTF32;
            }

            if (data.Slice(0, 4).SequenceEqual(Utf32BeBom))
            {
                bomLength = 4;
                return new System.Text.UTF32Encoding(bigEndian: true, byteOrderMark: true);
            }
        }

        // Check for UTF-8 BOM
        if (data.Length >= 3 && data.Slice(0, 3).SequenceEqual(Utf8Bom))
        {
            bomLength = 3;
            return System.Text.Encoding.UTF8;
        }

        // Check for UTF-16 BOMs
        if (data.Slice(0, 2).SequenceEqual(Utf16LeBom))
        {
            bomLength = 2;
            return System.Text.Encoding.Unicode;
        }

        if (data.Slice(0, 2).SequenceEqual(Utf16BeBom))
        {
            bomLength = 2;
            return System.Text.Encoding.BigEndianUnicode;
        }

        return null;
    }

    /// <summary>
    /// Gets the BOM bytes for the specified encoding.
    /// </summary>
    public static byte[]? GetBom(System.Text.Encoding encoding)
    {
        return encoding.GetPreamble();
    }

    /// <summary>
    /// Checks if the encoding has a BOM.
    /// </summary>
    public static bool HasBom(System.Text.Encoding encoding)
    {
        var preamble = encoding.GetPreamble();
        return preamble is not null && preamble.Length > 0;
    }
}
