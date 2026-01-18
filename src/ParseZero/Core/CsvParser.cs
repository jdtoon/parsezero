using System.Buffers;
using System.Runtime.CompilerServices;
using ParseZero.Encoding;
using ParseZero.Simd;

namespace ParseZero.Core;

/// <summary>
/// Core CSV parser that processes a stream and yields rows.
/// Uses ArrayPool for zero-allocation buffer management.
/// </summary>
internal sealed class CsvParser : IDisposable
{
    private readonly Stream _stream;
    private readonly CsvOptions _options;
    private readonly IDelimiterScanner _scanner;
    private readonly System.Text.Encoding _encoding;

    private byte[] _byteBuffer;
    private char[] _charBuffer;
    private int _charBufferLength;
    private int _charBufferPosition;
    private int _rowNumber;
    private bool _disposed;
    private bool _endOfStream;

    // For handling rows that span buffer boundaries
    private char[]? _lineBuffer;
    private int _lineBufferLength;

    public CsvParser(Stream stream, CsvOptions options)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        // Detect encoding from BOM or use specified encoding
        _encoding = options.Encoding ?? BomDetector.DetectEncoding(stream) ?? System.Text.Encoding.UTF8;

        // Initialize scanner based on platform
        _scanner = DelimiterScannerFactory.Create();

        // Rent buffers from the pool
        _byteBuffer = ArrayPool<byte>.Shared.Rent(options.BufferSize);
        _charBuffer = ArrayPool<char>.Shared.Rent(options.BufferSize * 2); // Chars can be larger than bytes for multi-byte encodings

        _charBufferLength = 0;
        _charBufferPosition = 0;
        _rowNumber = 0;
        _endOfStream = false;
    }

    /// <summary>
    /// Gets the current row number (1-based).
    /// </summary>
    public int RowNumber => _rowNumber;

    /// <summary>
    /// Tries to read the next row from the CSV.
    /// </summary>
    /// <param name="row">The parsed row.</param>
    /// <returns>True if a row was read, false if end of stream.</returns>
    public bool TryReadRow(out CsvRow row)
    {
        row = default;

        while (true)
        {
            // Try to find a complete line in the current buffer
            if (TryExtractLine(out var line))
            {
                _rowNumber++;

                // Skip empty lines if configured
                if (_options.SkipEmptyLines && line.IsEmpty)
                {
                    continue;
                }

                // Skip comment lines
                if (_options.CommentCharacter.HasValue && !line.IsEmpty &&
                    line[0] == _options.CommentCharacter.Value)
                {
                    continue;
                }

                // Skip header row
                if (_options.HasHeader && _rowNumber == 1)
                {
                    continue;
                }

                row = new CsvRow(line, _options, _rowNumber);
                return true;
            }

            // Need more data
            if (_endOfStream)
            {
                // Process any remaining data
                if (_lineBufferLength > 0 || _charBufferPosition < _charBufferLength)
                {
                    var remaining = GetRemainingData();
                    if (!remaining.IsEmpty)
                    {
                        _rowNumber++;

                        // Clear the line buffer
                        _lineBufferLength = 0;
                        _charBufferPosition = _charBufferLength;

                        if (_options.SkipEmptyLines && remaining.IsEmpty)
                        {
                            return false;
                        }

                        row = new CsvRow(remaining, _options, _rowNumber);
                        return true;
                    }
                }
                return false;
            }

            // Read more data
            if (!FillBuffer())
            {
                _endOfStream = true;
            }
        }
    }

    /// <summary>
    /// Tries to extract a complete line from the buffer.
    /// </summary>
    private bool TryExtractLine(out ReadOnlySpan<char> line)
    {
        line = default;

        var available = _charBuffer.AsSpan(_charBufferPosition, _charBufferLength - _charBufferPosition);
        if (available.IsEmpty)
        {
            return false;
        }

        // Use scanner to find line end
        int lineEnd = _scanner.FindLineEnd(available, _options);

        if (lineEnd == -1)
        {
            // No complete line found - accumulate in line buffer
            AccumulateToLineBuffer(available);
            _charBufferPosition = _charBufferLength;
            return false;
        }

        // Check max line length
        int totalLineLength = _lineBufferLength + lineEnd;
        if (totalLineLength > _options.MaxLineLength)
        {
            throw new InvalidDataException($"Line {_rowNumber + 1} exceeds maximum length of {_options.MaxLineLength} characters.");
        }

        // Complete line found
        if (_lineBufferLength > 0)
        {
            // Combine line buffer with current chunk
            AccumulateToLineBuffer(available.Slice(0, lineEnd));
            line = _lineBuffer.AsSpan(0, _lineBufferLength);
            _lineBufferLength = 0;
        }
        else
        {
            line = available.Slice(0, lineEnd);
        }

        // Advance past the line and line ending
        int skip = lineEnd + 1;
        if (lineEnd < available.Length - 1 &&
            available[lineEnd] == '\r' && available[lineEnd + 1] == '\n')
        {
            skip = lineEnd + 2;
        }

        _charBufferPosition += skip;

        // Handle case where line ends with just \r
        if (lineEnd < available.Length && available[lineEnd] == '\r' && skip == lineEnd + 1)
        {
            // Could be \r\n split across buffers - peek ahead
            // For now, treat standalone \r as line ending
        }

        return true;
    }

    private void AccumulateToLineBuffer(ReadOnlySpan<char> data)
    {
        int newLength = _lineBufferLength + data.Length;

        if (newLength > _options.MaxLineLength)
        {
            throw new InvalidDataException($"Line exceeds maximum length of {_options.MaxLineLength} characters.");
        }

        // Ensure line buffer is large enough
        if (_lineBuffer is null || _lineBuffer.Length < newLength)
        {
            var newBuffer = ArrayPool<char>.Shared.Rent(Math.Max(newLength, _options.MaxLineLength / 4));
            if (_lineBuffer is not null)
            {
                _lineBuffer.AsSpan(0, _lineBufferLength).CopyTo(newBuffer);
                ArrayPool<char>.Shared.Return(_lineBuffer);
            }
            _lineBuffer = newBuffer;
        }

        data.CopyTo(_lineBuffer.AsSpan(_lineBufferLength));
        _lineBufferLength = newLength;
    }

    private ReadOnlySpan<char> GetRemainingData()
    {
        if (_lineBufferLength > 0)
        {
            var remaining = _charBuffer.AsSpan(_charBufferPosition, _charBufferLength - _charBufferPosition);
            if (!remaining.IsEmpty)
            {
                AccumulateToLineBuffer(remaining);
            }
            return _lineBuffer.AsSpan(0, _lineBufferLength);
        }

        return _charBuffer.AsSpan(_charBufferPosition, _charBufferLength - _charBufferPosition);
    }

    /// <summary>
    /// Fills the buffer with more data from the stream.
    /// </summary>
    private bool FillBuffer()
    {
        // Move any remaining data to the beginning of the buffer
        if (_charBufferPosition > 0 && _charBufferPosition < _charBufferLength)
        {
            var remaining = _charBuffer.AsSpan(_charBufferPosition, _charBufferLength - _charBufferPosition);
            remaining.CopyTo(_charBuffer);
            _charBufferLength = remaining.Length;
            _charBufferPosition = 0;
        }
        else
        {
            _charBufferLength = 0;
            _charBufferPosition = 0;
        }

        // Read bytes
        int bytesRead = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);
        if (bytesRead == 0)
        {
            return false;
        }

        // Decode to chars
        int charsDecoded = _encoding.GetChars(
            _byteBuffer, 0, bytesRead,
            _charBuffer, _charBufferLength);

        _charBufferLength += charsDecoded;
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        ArrayPool<byte>.Shared.Return(_byteBuffer);
        ArrayPool<char>.Shared.Return(_charBuffer);

        if (_lineBuffer is not null)
        {
            ArrayPool<char>.Shared.Return(_lineBuffer);
        }
    }
}
