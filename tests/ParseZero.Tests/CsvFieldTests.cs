using Xunit;
using ParseZero;
using ParseZero.Core;

namespace ParseZero.Tests;

public class CsvFieldTests
{
    private static CsvRow GetFirstRow(string csv, CsvOptions options)
    {
        foreach (var row in CsvReader.Parse(csv, options))
        {
            return row;
        }
        throw new InvalidOperationException("No rows found");
    }

    [Fact]
    public void ParseInt32_ValidNumber_ReturnsCorrectValue()
    {
        // Arrange
        const string csv = "42,100,-5";

        // Act & Assert
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            Assert.Equal(42, row[0].ParseInt32());
            Assert.Equal(100, row[1].ParseInt32());
            Assert.Equal(-5, row[2].ParseInt32());
        }
    }

    [Fact]
    public void TryParseInt32_ValidNumber_ReturnsTrue()
    {
        // Arrange
        const string csv = "42";

        // Act & Assert
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            bool success = row[0].TryParseInt32(out int value);
            Assert.True(success);
            Assert.Equal(42, value);
        }
    }

    [Fact]
    public void TryParseInt32_InvalidNumber_ReturnsFalse()
    {
        // Arrange
        const string csv = "abc";

        // Act & Assert
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            bool success = row[0].TryParseInt32(out int value);
            Assert.False(success);
            Assert.Equal(0, value);
        }
    }

    [Fact]
    public void ParseDouble_ValidNumber_ReturnsCorrectValue()
    {
        // Arrange
        const string csv = "3.14,2.718,-1.5";

        // Act & Assert
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            Assert.Equal(3.14, row[0].ParseDouble(), precision: 5);
            Assert.Equal(2.718, row[1].ParseDouble(), precision: 5);
            Assert.Equal(-1.5, row[2].ParseDouble(), precision: 5);
        }
    }

    [Fact]
    public void ParseDecimal_ValidNumber_ReturnsCorrectValue()
    {
        // Arrange
        const string csv = "123.45,0.01,-99.99";

        // Act & Assert
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            Assert.Equal(123.45m, row[0].ParseDecimal());
            Assert.Equal(0.01m, row[1].ParseDecimal());
            Assert.Equal(-99.99m, row[2].ParseDecimal());
        }
    }

    [Fact]
    public void ParseBoolean_ValidValues_ReturnsCorrectValue()
    {
        // Arrange
        const string csv = "true,false,1,0,yes,no,Y,N";

        // Act & Assert
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            Assert.True(row[0].ParseBoolean());
            Assert.False(row[1].ParseBoolean());
            Assert.True(row[2].ParseBoolean());
            Assert.False(row[3].ParseBoolean());
            Assert.True(row[4].ParseBoolean());
            Assert.False(row[5].ParseBoolean());
            Assert.True(row[6].ParseBoolean());
            Assert.False(row[7].ParseBoolean());
        }
    }

    [Fact]
    public void ParseDateTime_ValidDate_ReturnsCorrectValue()
    {
        // Arrange
        const string csv = "2024-01-15";

        // Act & Assert
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            var date = row[0].ParseDateTime();
            Assert.Equal(2024, date.Year);
            Assert.Equal(1, date.Month);
            Assert.Equal(15, date.Day);
        }
    }

    [Fact]
    public void ParseDateTimeExact_WithFormat_ReturnsCorrectValue()
    {
        // Arrange
        const string csv = "15/01/2024";

        // Act & Assert
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            var date = row[0].ParseDateTimeExact("dd/MM/yyyy");
            Assert.Equal(2024, date.Year);
            Assert.Equal(1, date.Month);
            Assert.Equal(15, date.Day);
        }
    }

    [Fact]
    public void ParseGuid_ValidGuid_ReturnsCorrectValue()
    {
        // Arrange
        var expected = Guid.NewGuid();
        var csv = expected.ToString();

        // Act & Assert
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            var actual = row[0].ParseGuid();
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void IsEmpty_EmptyField_ReturnsTrue()
    {
        // Arrange
        const string csv = ",value,";

        // Act & Assert
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            Assert.True(row[0].IsEmpty);
            Assert.False(row[1].IsEmpty);
            Assert.True(row[2].IsEmpty);
        }
    }

    [Fact]
    public void Length_ReturnsCorrectLength()
    {
        // Arrange
        const string csv = "hello,world,test123";

        // Act & Assert
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            Assert.Equal(5, row[0].Length);
            Assert.Equal(5, row[1].Length);
            Assert.Equal(7, row[2].Length);
        }
    }

    [Fact]
    public void ToString_ReturnsStringValue()
    {
        // Arrange
        const string csv = "hello,world";

        // Act & Assert
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            Assert.Equal("hello", row[0].ToString());
            Assert.Equal("world", row[1].ToString());
        }
    }

    [Fact]
    public void ParseInt64_ValidNumber_ReturnsCorrectValue()
    {
        // Arrange
        const string csv = "9223372036854775807,-9223372036854775808";

        // Act & Assert
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            Assert.Equal(long.MaxValue, row[0].ParseInt64());
            Assert.Equal(long.MinValue, row[1].ParseInt64());
        }
    }

    [Fact]
    public void Span_ReturnsReadOnlySpan()
    {
        // Arrange
        const string csv = "hello";

        // Act & Assert
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            var span = row[0].Span;
            Assert.Equal(5, span.Length);
            Assert.Equal('h', span[0]);
            Assert.Equal('o', span[4]);
        }
    }

    [Fact]
    public void TryParseDouble_ValidNumber_ReturnsTrue()
    {
        // Arrange
        const string csv = "3.14159";

        // Act & Assert
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            bool success = row[0].TryParseDouble(out double value);
            Assert.True(success);
            Assert.Equal(3.14159, value, precision: 5);
        }
    }

    [Fact]
    public void TryParseDecimal_ValidNumber_ReturnsTrue()
    {
        // Arrange
        const string csv = "123.45";

        // Act & Assert
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            bool success = row[0].TryParseDecimal(out decimal value);
            Assert.True(success);
            Assert.Equal(123.45m, value);
        }
    }
}
