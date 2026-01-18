using Xunit;
using ParseZero;
using ParseZero.Core;

namespace ParseZero.Tests;

public class QuotedFieldTests
{
    private static List<(int FieldCount, string[] Fields)> ParseToList(string csv, CsvOptions options)
    {
        var result = new List<(int, string[])>();
        foreach (var row in CsvReader.Parse(csv, options))
        {
            var fields = new string[row.FieldCount];
            for (int i = 0; i < row.FieldCount; i++)
            {
                fields[i] = row[i].ToString();
            }
            result.Add((row.FieldCount, fields));
        }
        return result;
    }

    [Fact]
    public void Parse_QuotedField_ReturnsUnquotedValue()
    {
        // Arrange
        const string csv = "\"hello\",world";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false });

        // Assert
        Assert.Single(rows);
        Assert.Equal("hello", rows[0].Fields[0]);
        Assert.Equal("world", rows[0].Fields[1]);
    }

    [Fact]
    public void Parse_QuotedFieldWithComma_PreservesComma()
    {
        // Arrange
        const string csv = "\"hello, world\",test";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false });

        // Assert
        Assert.Single(rows);
        Assert.Equal(2, rows[0].FieldCount);
        Assert.Equal("hello, world", rows[0].Fields[0]);
    }

    [Fact]
    public void Parse_QuotedFieldWithNewline_PreservesNewline()
    {
        // Arrange
        const string csv = "\"hello\nworld\",test";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false });

        // Assert
        Assert.Single(rows);
        Assert.Equal("hello\nworld", rows[0].Fields[0]);
    }

    [Fact]
    public void Parse_EscapedQuotes_UnescapesCorrectly()
    {
        // Arrange
        const string csv = "\"he said \"\"hello\"\"\",test";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false });

        // Assert
        Assert.Single(rows);
        Assert.Equal("he said \"hello\"", rows[0].Fields[0]);
    }

    [Fact]
    public void Parse_EmptyQuotedField_ReturnsEmpty()
    {
        // Arrange
        const string csv = "\"\",test";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false });

        // Assert
        Assert.Single(rows);
        // Empty quoted field becomes empty string
        Assert.Equal("", rows[0].Fields[0]);
        Assert.Equal("test", rows[0].Fields[1]);
    }

    [Fact]
    public void Parse_AllQuotedFields_WorksCorrectly()
    {
        // Arrange
        const string csv = "\"a\",\"b\",\"c\"\n\"1\",\"2\",\"3\"";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false });

        // Assert
        Assert.Equal(2, rows.Count);
        Assert.Equal("a", rows[0].Fields[0]);
        Assert.Equal("2", rows[1].Fields[1]);
    }

    [Fact]
    public void Parse_QuotesDisabled_TreatsQuotesLiterally()
    {
        // Arrange
        const string csv = "\"hello\",world";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false, AllowQuotedFields = false });

        // Assert
        Assert.Single(rows);
        Assert.Equal("\"hello\"", rows[0].Fields[0]);
    }

    [Fact]
    public void Parse_MixedQuotedAndUnquoted_WorksCorrectly()
    {
        // Arrange
        const string csv = "\"quoted\",unquoted,\"also quoted\"";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false });

        // Assert
        Assert.Single(rows);
        Assert.Equal(3, rows[0].FieldCount);
        Assert.Equal("quoted", rows[0].Fields[0]);
        Assert.Equal("unquoted", rows[0].Fields[1]);
        Assert.Equal("also quoted", rows[0].Fields[2]);
    }

    [Fact]
    public void Parse_QuotedFieldWithCarriageReturn_PreservesCR()
    {
        // Arrange
        const string csv = "\"hello\r\nworld\",test";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false });

        // Assert
        Assert.Single(rows);
        Assert.Contains("\r\n", rows[0].Fields[0]);
    }

    [Fact]
    public void Parse_DoubleQuotesOnly_ReturnsQuote()
    {
        // Arrange
        const string csv = "\"\"\"\",test";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false });

        // Assert
        Assert.Single(rows);
        Assert.Equal("\"", rows[0].Fields[0]);
    }
}
