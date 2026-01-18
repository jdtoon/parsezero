namespace ParseZero;

/// <summary>
/// Configuration options for CSV parsing.
/// </summary>
public sealed class CsvOptions
{
    /// <summary>
    /// The field delimiter character. Default is comma (,).
    /// </summary>
    public char Delimiter { get; set; } = ',';

    /// <summary>
    /// The quote character used for escaping fields. Default is double quote (").
    /// </summary>
    public char Quote { get; set; } = '"';

    /// <summary>
    /// Whether the first row contains column headers. Default is true.
    /// </summary>
    public bool HasHeader { get; set; } = true;

    /// <summary>
    /// The encoding to use when reading the file. Default is UTF-8.
    /// If null, encoding will be auto-detected from BOM.
    /// </summary>
    public System.Text.Encoding? Encoding { get; set; }

    /// <summary>
    /// The buffer size for reading data. Default is 4096 bytes.
    /// </summary>
    public int BufferSize { get; set; } = 4096;

    /// <summary>
    /// Maximum allowed line length in characters. Default is 64KB.
    /// This is a security measure to prevent denial-of-service from malformed files.
    /// </summary>
    public int MaxLineLength { get; set; } = 64 * 1024;

    /// <summary>
    /// Whether to trim whitespace from field values. Default is false.
    /// </summary>
    public bool TrimFields { get; set; }

    /// <summary>
    /// Whether to allow quoted fields (RFC 4180 compliant). Default is true.
    /// </summary>
    public bool AllowQuotedFields { get; set; } = true;

    /// <summary>
    /// The expected number of columns. If set, rows with different column counts will raise an error.
    /// Default is null (auto-detect from first row).
    /// </summary>
    public int? ExpectedColumnCount { get; set; }

    /// <summary>
    /// Whether to skip empty lines. Default is true.
    /// </summary>
    public bool SkipEmptyLines { get; set; } = true;

    /// <summary>
    /// The comment character. Lines starting with this character are skipped.
    /// Default is null (no comment support).
    /// </summary>
    public char? CommentCharacter { get; set; }

    /// <summary>
    /// Creates a new instance with default options.
    /// </summary>
    public static CsvOptions Default => new();

    /// <summary>
    /// Creates options optimized for RFC 4180 compliant CSV files.
    /// </summary>
    public static CsvOptions Rfc4180 => new()
    {
        Delimiter = ',',
        Quote = '"',
        AllowQuotedFields = true,
        TrimFields = false
    };

    /// <summary>
    /// Creates options for tab-separated values (TSV) files.
    /// </summary>
    public static CsvOptions Tsv => new()
    {
        Delimiter = '\t',
        AllowQuotedFields = true
    };
}
