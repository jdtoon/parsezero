using System.Text;
using ParseZero;
using PzEncodingProvider = ParseZero.Encoding.EncodingProvider;
using Xunit;

namespace ParseZero.Tests;

public class EncodingTests
{
    [Fact]
    public void Utf8Bom_IsDetectedAndParsed()
    {
        // Arrange
        const string content = "Col1,Col2\nhello,Δ";
        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(content)).ToArray();
        using var stream = new MemoryStream(bytes);

        // Act
        var rows = new List<string[]>();
        foreach (var row in CsvReader.Read(stream, new CsvOptions { HasHeader = true }))
        {
            rows.Add(new[] { row[0].ToString(), row[1].ToString() });
        }

        // Assert
        Assert.Single(rows);
        Assert.Equal("hello", rows[0][0]);
        Assert.Equal("Δ", rows[0][1]);
    }

    [Fact]
    public void Utf16LeBom_IsDetectedAndParsed()
    {
        // Arrange
        const string content = "Col1,Col2\nää,ßß";
        var encoding = System.Text.Encoding.Unicode; // UTF-16 LE with BOM
        var bytes = encoding.GetPreamble().Concat(encoding.GetBytes(content)).ToArray();
        using var stream = new MemoryStream(bytes);

        // Act
        var rows = new List<string[]>();
        foreach (var row in CsvReader.Read(stream, new CsvOptions { HasHeader = true }))
        {
            rows.Add(new[] { row[0].ToString(), row[1].ToString() });
        }

        // Assert
        Assert.Single(rows);
        Assert.Equal("ää", rows[0][0]);
        Assert.Equal("ßß", rows[0][1]);
    }

    [Fact]
    public void Latin1Encoding_ParsesUsingConfiguredEncoding()
    {
        // Arrange
        const string content = "Word\ncafé"; // café encoded as ISO-8859-1
        var encoding = PzEncodingProvider.Iso88591;
        var bytes = encoding.GetBytes(content);
        using var stream = new MemoryStream(bytes);
        var options = new CsvOptions { HasHeader = true, Encoding = encoding };

        // Act
        var rows = new List<string[]>();
        foreach (var row in CsvReader.Read(stream, options))
        {
            rows.Add(new[] { row[0].ToString() });
        }

        // Assert
        Assert.Single(rows);
        Assert.Equal("café", rows[0][0]);
    }

    [Fact]
    public void TrimFields_RemovesOuterWhitespace()
    {
        // Arrange
        const string csv = "  a  ,  b  ,\"  c  \"";
        var options = new CsvOptions { HasHeader = false, TrimFields = true };

        // Act
        var rows = new List<string[]>();
        foreach (var row in CsvReader.Parse(csv, options))
        {
            rows.Add(new[]
            {
                row[0].ToString(),
                row[1].ToString(),
                row[2].ToString()
            });
        }

        // Assert
        Assert.Single(rows);
        Assert.Equal("a", rows[0][0]);
        Assert.Equal("b", rows[0][1]);
        Assert.Equal("  c  ", rows[0][2]);
    }

    [Fact]
    public void CarriageReturnOnlyLineEndings_AreHandled()
    {
        // Arrange
        const string csv = "a,b,c\rc,d,e";
        var options = new CsvOptions { HasHeader = false };

        // Act
        var rows = new List<string[]>();
        foreach (var row in CsvReader.Parse(csv, options))
        {
            var fields = new string[row.FieldCount];
            for (int i = 0; i < row.FieldCount; i++)
            {
                fields[i] = row[i].ToString();
            }
            rows.Add(fields);
        }

        // Assert
        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { "a", "b", "c" }, rows[0]);
        Assert.Equal(new[] { "c", "d", "e" }, rows[1]);
    }
}
