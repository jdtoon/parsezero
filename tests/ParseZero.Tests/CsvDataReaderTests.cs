using Xunit;
using ParseZero.Data;
using ParseZero;
using System.Data;

namespace ParseZero.Tests;

public class CsvDataReaderTests
{
    [Fact]
    public void Read_SimpleData_ReadsAllRows()
    {
        // Arrange
        var csv = "Name,Age,City\nJohn,30,NYC\nJane,25,LA\nBob,35,Chicago";
        using var stream = CreateStream(csv);
        using var reader = CsvDataReader.Create(stream, new CsvOptions { HasHeader = true });

        // Act
        var rows = new List<string[]>();
        while (reader.Read())
        {
            rows.Add(new[] { reader.GetString(0), reader.GetString(1), reader.GetString(2) });
        }

        // Assert
        Assert.Equal(3, rows.Count);
        Assert.Equal("John", rows[0][0]);
        Assert.Equal("30", rows[0][1]);
        Assert.Equal("NYC", rows[0][2]);
    }

    [Fact]
    public void FieldCount_ReturnsCorrectCount()
    {
        // Arrange
        var csv = "A,B,C\n1,2,3";
        using var stream = CreateStream(csv);
        using var reader = CsvDataReader.Create(stream, new CsvOptions { HasHeader = true });

        // Act
        reader.Read();

        // Assert
        Assert.Equal(3, reader.FieldCount);
    }

    [Fact]
    public void GetName_ReturnsHeaderNames()
    {
        // Arrange
        var csv = "Name,Age,City\n1,2,3";
        using var stream = CreateStream(csv);
        using var reader = CsvDataReader.Create(stream, new CsvOptions { HasHeader = true });

        // Act
        reader.Read();

        // Assert
        Assert.Equal("Name", reader.GetName(0));
        Assert.Equal("Age", reader.GetName(1));
        Assert.Equal("City", reader.GetName(2));
    }

    [Fact]
    public void GetOrdinal_ReturnsCorrectIndex()
    {
        // Arrange
        var csv = "Name,Age,City\n1,2,3";
        using var stream = CreateStream(csv);
        using var reader = CsvDataReader.Create(stream, new CsvOptions { HasHeader = true });

        // Act
        reader.Read();

        // Assert
        Assert.Equal(0, reader.GetOrdinal("Name"));
        Assert.Equal(1, reader.GetOrdinal("Age"));
        Assert.Equal(2, reader.GetOrdinal("City"));
    }

    [Fact]
    public void GetOrdinal_CaseInsensitive()
    {
        // Arrange
        var csv = "Name,Age,City\n1,2,3";
        using var stream = CreateStream(csv);
        using var reader = CsvDataReader.Create(stream, new CsvOptions { HasHeader = true });

        // Act
        reader.Read();

        // Assert
        Assert.Equal(0, reader.GetOrdinal("name"));
        Assert.Equal(1, reader.GetOrdinal("AGE"));
        Assert.Equal(2, reader.GetOrdinal("CITY"));
    }

    [Fact]
    public void IsDBNull_EmptyField_ReturnsTrue()
    {
        // Arrange
        var csv = "A,B,C\n1,,3";
        using var stream = CreateStream(csv);
        using var reader = CsvDataReader.Create(stream, new CsvOptions { HasHeader = true });

        // Act
        reader.Read();

        // Assert
        Assert.False(reader.IsDBNull(0));
        Assert.True(reader.IsDBNull(1));
        Assert.False(reader.IsDBNull(2));
    }

    [Fact]
    public void GetInt32_ParsesCorrectly()
    {
        // Arrange
        var csv = "Value\n42";
        using var stream = CreateStream(csv);
        using var reader = CsvDataReader.Create(stream, new CsvOptions { HasHeader = true });

        // Act
        reader.Read();

        // Assert
        Assert.Equal(42, reader.GetInt32(0));
    }

    [Fact]
    public void GetDecimal_ParsesCorrectly()
    {
        // Arrange
        var csv = "Value\n123.45";
        using var stream = CreateStream(csv);
        using var reader = CsvDataReader.Create(stream, new CsvOptions { HasHeader = true });

        // Act
        reader.Read();

        // Assert
        Assert.Equal(123.45m, reader.GetDecimal(0));
    }

    [Fact]
    public void GetBoolean_ParsesCorrectly()
    {
        // Arrange
        var csv = "A,B,C,D\ntrue,false,1,0";
        using var stream = CreateStream(csv);
        using var reader = CsvDataReader.Create(stream, new CsvOptions { HasHeader = true });

        // Act
        reader.Read();

        // Assert
        Assert.True(reader.GetBoolean(0));
        Assert.False(reader.GetBoolean(1));
        Assert.True(reader.GetBoolean(2));
        Assert.False(reader.GetBoolean(3));
    }

    [Fact]
    public void GetDateTime_ParsesCorrectly()
    {
        // Arrange
        var csv = "Date\n2024-01-15";
        using var stream = CreateStream(csv);
        using var reader = CsvDataReader.Create(stream, new CsvOptions { HasHeader = true });

        // Act
        reader.Read();
        var date = reader.GetDateTime(0);

        // Assert
        Assert.Equal(2024, date.Year);
        Assert.Equal(1, date.Month);
        Assert.Equal(15, date.Day);
    }

    [Fact]
    public void Indexer_ByName_ReturnsCorrectValue()
    {
        // Arrange
        var csv = "Name,Age\nJohn,30";
        using var stream = CreateStream(csv);
        using var reader = CsvDataReader.Create(stream, new CsvOptions { HasHeader = true });

        // Act
        reader.Read();

        // Assert
        Assert.Equal("John", reader["Name"]);
        Assert.Equal("30", reader["Age"]);
    }

    [Fact]
    public void Indexer_ByIndex_ReturnsCorrectValue()
    {
        // Arrange
        var csv = "Name,Age\nJohn,30";
        using var stream = CreateStream(csv);
        using var reader = CsvDataReader.Create(stream, new CsvOptions { HasHeader = true });

        // Act
        reader.Read();

        // Assert
        Assert.Equal("John", reader[0]);
        Assert.Equal("30", reader[1]);
    }

    [Fact]
    public void GetSchemaTable_ReturnsValidSchema()
    {
        // Arrange
        var csv = "Name,Age,City\nJohn,30,NYC";
        using var stream = CreateStream(csv);
        using var reader = CsvDataReader.Create(stream, new CsvOptions { HasHeader = true });

        // Act
        reader.Read();
        var schema = reader.GetSchemaTable();

        // Assert
        Assert.NotNull(schema);
        Assert.Equal(3, schema.Rows.Count);
        Assert.Equal("Name", schema.Rows[0]["ColumnName"]);
        Assert.Equal("Age", schema.Rows[1]["ColumnName"]);
        Assert.Equal("City", schema.Rows[2]["ColumnName"]);
    }

    [Fact]
    public void GetValues_FillsArray()
    {
        // Arrange
        var csv = "A,B,C\n1,2,3";
        using var stream = CreateStream(csv);
        using var reader = CsvDataReader.Create(stream, new CsvOptions { HasHeader = true });

        // Act
        reader.Read();
        var values = new object[3];
        int count = reader.GetValues(values);

        // Assert
        Assert.Equal(3, count);
        Assert.Equal("1", values[0]);
        Assert.Equal("2", values[1]);
        Assert.Equal("3", values[2]);
    }

    [Fact]
    public void RecordsAffected_CountsRows()
    {
        // Arrange
        var csv = "A\n1\n2\n3";
        using var stream = CreateStream(csv);
        using var reader = CsvDataReader.Create(stream, new CsvOptions { HasHeader = true });

        // Act
        while (reader.Read()) { }

        // Assert
        Assert.Equal(3, reader.RecordsAffected);
    }

    [Fact]
    public void Close_SetsIsClosed()
    {
        // Arrange
        var csv = "A\n1";
        using var stream = CreateStream(csv);
        var reader = CsvDataReader.Create(stream, new CsvOptions { HasHeader = true });

        // Act
        Assert.False(reader.IsClosed);
        reader.Close();

        // Assert
        Assert.True(reader.IsClosed);
    }

    private static MemoryStream CreateStream(string content)
    {
        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
    }
}
