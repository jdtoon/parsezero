using Xunit;
using ParseZero;
using ParseZero.Core;

namespace ParseZero.Tests;

public class CsvReaderTests
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
    public void Parse_SimpleRow_ReturnsCorrectFields()
    {
        // Arrange
        const string csv = "a,b,c";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false });

        // Assert
        Assert.Single(rows);
        Assert.Equal(3, rows[0].FieldCount);
        Assert.Equal("a", rows[0].Fields[0]);
        Assert.Equal("b", rows[0].Fields[1]);
        Assert.Equal("c", rows[0].Fields[2]);
    }

    [Fact]
    public void Parse_MultipleRows_ReturnsAllRows()
    {
        // Arrange
        const string csv = "a,b,c\n1,2,3\nx,y,z";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false });

        // Assert
        Assert.Equal(3, rows.Count);
    }

    [Fact]
    public void Parse_WithHeader_SkipsHeaderRow()
    {
        // Arrange
        const string csv = "Name,Age,City\nJohn,30,NYC\nJane,25,LA";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = true });

        // Assert
        Assert.Equal(2, rows.Count);
        Assert.Equal("John", rows[0].Fields[0]);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsNoRows()
    {
        // Arrange
        const string csv = "";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false });

        // Assert
        Assert.Empty(rows);
    }

    [Fact]
    public void Parse_EmptyLines_SkipsEmptyLines()
    {
        // Arrange
        const string csv = "a,b,c\n\n1,2,3\n\n";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false, SkipEmptyLines = true });

        // Assert
        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void Parse_EmptyLines_IncludesEmptyLinesWhenConfigured()
    {
        // Arrange
        const string csv = "a,b,c\n\n1,2,3";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false, SkipEmptyLines = false });

        // Assert
        Assert.Equal(3, rows.Count);
        Assert.Equal(0, rows[1].FieldCount);
    }

    [Fact]
    public void Parse_CommentLines_SkipsComments()
    {
        // Arrange
        const string csv = "#comment\na,b,c\n#another comment\n1,2,3";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false, CommentCharacter = '#' });

        // Assert
        Assert.Equal(2, rows.Count);
        Assert.Equal("a", rows[0].Fields[0]);
        Assert.Equal("1", rows[1].Fields[0]);
    }

    [Fact]
    public void Parse_DifferentDelimiter_UsesCustomDelimiter()
    {
        // Arrange
        const string csv = "a;b;c\n1;2;3";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false, Delimiter = ';' });

        // Assert
        Assert.Equal(2, rows.Count);
        Assert.Equal(3, rows[0].FieldCount);
        Assert.Equal("a", rows[0].Fields[0]);
    }

    [Fact]
    public void Parse_TabDelimited_WorksCorrectly()
    {
        // Arrange
        var csv = "a\tb\tc\n1\t2\t3";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false, Delimiter = '\t' });

        // Assert
        Assert.Equal(2, rows.Count);
        Assert.Equal(3, rows[0].FieldCount);
    }

    [Fact]
    public void Parse_WindowsLineEndings_HandlesCorrectly()
    {
        // Arrange
        const string csv = "a,b,c\r\n1,2,3\r\nx,y,z";

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false });

        // Assert
        Assert.Equal(3, rows.Count);
    }

    [Fact]
    public void Parse_LargeFieldCount_WorksCorrectly()
    {
        // Arrange
        var fields = string.Join(",", Enumerable.Range(1, 100).Select(i => $"f{i}"));
        var csv = fields;

        // Act
        var rows = ParseToList(csv, new CsvOptions { HasHeader = false });

        // Assert
        Assert.Single(rows);
        Assert.Equal(100, rows[0].FieldCount);
    }

    [Fact]
    public void Parse_FieldAccess_ByIndex_WorksCorrectly()
    {
        // Arrange
        const string csv = "Name,Age,City\nJohn,30,NYC";

        // Act & Assert
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = true }))
        {
            Assert.Equal("John", row[0].ToString());
            Assert.Equal("30", row[1].ToString());
            Assert.Equal("NYC", row[2].ToString());
        }
    }

    [Fact]
    public void Parse_RowCount_TracksCorrectly()
    {
        // Arrange
        const string csv = "a,b,c\n1,2,3\nx,y,z";

        // Act
        int rowNumber = 0;
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            rowNumber = row.RowNumber;
        }

        // Assert
        Assert.Equal(3, rowNumber);
    }
}
