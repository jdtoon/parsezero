using ParseZero.Core;

namespace ParseZero;

/// <summary>
/// Delegate for processing CSV rows.
/// </summary>
/// <param name="row">The current row.</param>
/// <returns>True to continue processing, false to stop.</returns>
public delegate bool CsvRowHandler(in CsvRow row);

/// <summary>
/// High-performance zero-allocation CSV reader.
/// </summary>
public static class CsvReader
{
    /// <summary>
    /// Reads a CSV file synchronously. Uses foreach pattern with custom enumerator.
    /// </summary>
    /// <param name="filePath">Path to the CSV file.</param>
    /// <param name="options">Optional parsing options.</param>
    /// <returns>A CsvFileReader that can be enumerated with foreach.</returns>
    public static CsvFileReader Read(string filePath, CsvOptions? options = null)
    {
        options ??= CsvOptions.Default;
        return new CsvFileReader(filePath, options);
    }

    /// <summary>
    /// Reads a CSV file synchronously from a stream.
    /// </summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="options">Optional parsing options.</param>
    /// <returns>A CsvStreamReader that can be enumerated with foreach.</returns>
    public static CsvStreamReader Read(Stream stream, CsvOptions? options = null)
    {
        options ??= CsvOptions.Default;
        return new CsvStreamReader(stream, options);
    }

    /// <summary>
    /// Parses a CSV string directly.
    /// </summary>
    /// <param name="csvContent">The CSV content as a string.</param>
    /// <param name="options">Optional parsing options.</param>
    /// <returns>A CsvStringReader that can be enumerated with foreach.</returns>
    public static CsvStringReader Parse(string csvContent, CsvOptions? options = null)
    {
        options ??= CsvOptions.Default;
        return new CsvStringReader(csvContent, options);
    }

    /// <summary>
    /// Parses CSV data from a ReadOnlySpan.
    /// </summary>
    /// <param name="data">The CSV data.</param>
    /// <param name="options">Optional parsing options.</param>
    /// <returns>An enumerator for CSV rows.</returns>
    public static CsvRowEnumerator ParseSpan(ReadOnlySpan<char> data, CsvOptions? options = null)
    {
        options ??= CsvOptions.Default;
        return new CsvRowEnumerator(data, options);
    }

    /// <summary>
    /// Reads a CSV file and invokes a handler for each row.
    /// This pattern allows zero-allocation row processing.
    /// </summary>
    /// <param name="filePath">Path to the CSV file.</param>
    /// <param name="handler">Handler invoked for each row.</param>
    /// <param name="options">Optional parsing options.</param>
    public static void ForEach(string filePath, CsvRowHandler handler, CsvOptions? options = null)
    {
        options ??= CsvOptions.Default;

        using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            options.BufferSize,
            FileOptions.SequentialScan);

        using var parser = new CsvParser(stream, options);

        while (parser.TryReadRow(out var row))
        {
            if (!handler(in row))
            {
                break;
            }
        }
    }

    /// <summary>
    /// Reads a CSV stream and invokes a handler for each row.
    /// This pattern allows zero-allocation row processing.
    /// </summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="handler">Handler invoked for each row.</param>
    /// <param name="options">Optional parsing options.</param>
    public static void ForEach(Stream stream, CsvRowHandler handler, CsvOptions? options = null)
    {
        options ??= CsvOptions.Default;

        using var parser = new CsvParser(stream, options);

        while (parser.TryReadRow(out var row))
        {
            if (!handler(in row))
            {
                break;
            }
        }
    }
}

/// <summary>
/// Enumerator for parsing CSV rows from a span.
/// </summary>
public ref struct CsvRowEnumerator
{
    private ReadOnlySpan<char> _remaining;
    private readonly CsvOptions _options;
    private CsvRow _current;
    private int _rowNumber;
    private bool _headerSkipped;

    internal CsvRowEnumerator(ReadOnlySpan<char> data, CsvOptions options)
    {
        _remaining = data;
        _options = options;
        _current = default;
        _rowNumber = 0;
        _headerSkipped = !options.HasHeader;
    }

    /// <summary>
    /// Gets the current row.
    /// </summary>
    public readonly CsvRow Current => _current;

    /// <summary>
    /// Advances to the next row.
    /// </summary>
    public bool MoveNext()
    {
        while (!_remaining.IsEmpty)
        {
            var lineEnd = FindLineEnd(_remaining);
            var line = lineEnd == -1 ? _remaining : _remaining.Slice(0, lineEnd);

            // Advance past this line
            if (lineEnd == -1)
            {
                _remaining = ReadOnlySpan<char>.Empty;
            }
            else
            {
                // Skip \r\n or \n
                int skip = lineEnd + 1;
                if (lineEnd > 0 && lineEnd < _remaining.Length - 1 &&
                    _remaining[lineEnd] == '\r' && _remaining[lineEnd + 1] == '\n')
                {
                    skip = lineEnd + 2;
                    line = _remaining.Slice(0, lineEnd);
                }
                else if (_remaining[lineEnd] == '\r' || _remaining[lineEnd] == '\n')
                {
                    line = _remaining.Slice(0, lineEnd);
                }

                _remaining = _remaining.Slice(Math.Min(skip, _remaining.Length));
            }

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
            if (!_headerSkipped)
            {
                _headerSkipped = true;
                continue;
            }

            _current = new CsvRow(line, _options, _rowNumber);
            return true;
        }

        return false;
    }

    private static int FindLineEnd(ReadOnlySpan<char> span)
    {
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] == '\n' || span[i] == '\r')
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Returns this enumerator.
    /// </summary>
    public readonly CsvRowEnumerator GetEnumerator() => this;
}

/// <summary>
/// Reader for CSV files that supports foreach enumeration.
/// </summary>
public sealed class CsvFileReader : IDisposable
{
    private readonly string _filePath;
    private readonly CsvOptions _options;

    internal CsvFileReader(string filePath, CsvOptions options)
    {
        _filePath = filePath;
        _options = options;
    }

    /// <summary>
    /// Gets an enumerator for the CSV rows.
    /// </summary>
    public Enumerator GetEnumerator() => new Enumerator(_filePath, _options);

    /// <inheritdoc />
    public void Dispose() { }

    /// <summary>
    /// Enumerator for CSV file rows.
    /// </summary>
    public ref struct Enumerator
    {
        private FileStream? _stream;
        private CsvParser? _parser;
        private CsvRow _current;

        internal Enumerator(string filePath, CsvOptions options)
        {
            _stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                options.BufferSize,
                FileOptions.SequentialScan);
            _parser = new CsvParser(_stream, options);
            _current = default;
        }

        /// <summary>
        /// Gets the current row.
        /// </summary>
        public readonly CsvRow Current => _current;

        /// <summary>
        /// Advances to the next row.
        /// </summary>
        public bool MoveNext()
        {
            if (_parser is null)
            {
                return false;
            }
            return _parser.TryReadRow(out _current);
        }

        /// <summary>
        /// Disposes the enumerator.
        /// </summary>
        public void Dispose()
        {
            _parser?.Dispose();
            _parser = null;
            _stream?.Dispose();
            _stream = null;
        }
    }
}

/// <summary>
/// Reader for CSV streams that supports foreach enumeration.
/// </summary>
public sealed class CsvStreamReader : IDisposable
{
    private readonly Stream _stream;
    private readonly CsvOptions _options;

    internal CsvStreamReader(Stream stream, CsvOptions options)
    {
        _stream = stream;
        _options = options;
    }

    /// <summary>
    /// Gets an enumerator for the CSV rows.
    /// </summary>
    public Enumerator GetEnumerator() => new Enumerator(_stream, _options);

    /// <inheritdoc />
    public void Dispose() { }

    /// <summary>
    /// Enumerator for CSV stream rows.
    /// </summary>
    public ref struct Enumerator
    {
        private CsvParser? _parser;
        private CsvRow _current;

        internal Enumerator(Stream stream, CsvOptions options)
        {
            _parser = new CsvParser(stream, options);
            _current = default;
        }

        /// <summary>
        /// Gets the current row.
        /// </summary>
        public readonly CsvRow Current => _current;

        /// <summary>
        /// Advances to the next row.
        /// </summary>
        public bool MoveNext()
        {
            if (_parser is null)
            {
                return false;
            }
            return _parser.TryReadRow(out _current);
        }

        /// <summary>
        /// Disposes the enumerator.
        /// </summary>
        public void Dispose()
        {
            _parser?.Dispose();
            _parser = null;
        }
    }
}

/// <summary>
/// Reader for CSV strings that supports foreach enumeration.
/// </summary>
public sealed class CsvStringReader : IDisposable
{
    private readonly string _content;
    private readonly CsvOptions _options;

    internal CsvStringReader(string content, CsvOptions options)
    {
        _content = content;
        _options = options;
    }

    /// <summary>
    /// Gets an enumerator for the CSV rows.
    /// </summary>
    public Enumerator GetEnumerator() => new Enumerator(_content, _options);

    /// <inheritdoc />
    public void Dispose() { }

    /// <summary>
    /// Enumerator for CSV string rows.
    /// </summary>
    public ref struct Enumerator
    {
        private MemoryStream? _stream;
        private CsvParser? _parser;
        private CsvRow _current;

        internal Enumerator(string content, CsvOptions options)
        {
            var encoding = options.Encoding ?? System.Text.Encoding.UTF8;
            var bytes = encoding.GetBytes(content);
            _stream = new MemoryStream(bytes);
            _parser = new CsvParser(_stream, options);
            _current = default;
        }

        /// <summary>
        /// Gets the current row.
        /// </summary>
        public readonly CsvRow Current => _current;

        /// <summary>
        /// Advances to the next row.
        /// </summary>
        public bool MoveNext()
        {
            if (_parser is null)
            {
                return false;
            }
            return _parser.TryReadRow(out _current);
        }

        /// <summary>
        /// Disposes the enumerator.
        /// </summary>
        public void Dispose()
        {
            _parser?.Dispose();
            _parser = null;
            _stream?.Dispose();
            _stream = null;
        }
    }
}
