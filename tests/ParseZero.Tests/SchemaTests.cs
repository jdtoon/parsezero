using Xunit;
using ParseZero;
using ParseZero.Schema;

namespace ParseZero.Tests;

public class SchemaTests
{
    public class TestRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public DateTime Date { get; set; }
        public bool IsActive { get; set; }
    }

    [Fact]
    public void Schema_MapsBasicTypes()
    {
        // Arrange
        var csv = "Id,Name,Price,Date,IsActive\n1,Test,99.99,2024-01-15,true";
        var schema = ParseZero.Schema.Schema.For<TestRecord>()
            .Column(t => t.Id)
            .Column(t => t.Name)
            .Column(t => t.Price)
            .Column(t => t.Date)
            .Column(t => t.IsActive)
            .Build();

        // Act
        TestRecord? record = null;
        bool headerRead = false;
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            if (!headerRead)
            {
                schema.MapHeader(row);
                headerRead = true;
                continue;
            }
            record = schema.Map(row);
        }

        // Assert
        Assert.NotNull(record);
        Assert.Equal(1, record.Id);
        Assert.Equal("Test", record.Name);
        Assert.Equal(99.99m, record.Price);
        Assert.Equal(new DateTime(2024, 1, 15), record.Date);
        Assert.True(record.IsActive);
    }

    [Fact]
    public void Schema_ColumnCount_ReturnsCorrectCount()
    {
        // Arrange
        var schema = ParseZero.Schema.Schema.For<TestRecord>()
            .Column(t => t.Id)
            .Column(t => t.Name)
            .Column(t => t.Price)
            .Build();

        // Assert
        Assert.Equal(3, schema.ColumnCount);
    }

    [Fact]
    public void Schema_GetColumnNames_ReturnsNames()
    {
        // Arrange
        var schema = ParseZero.Schema.Schema.For<TestRecord>()
            .Column(t => t.Id)
            .Column(t => t.Name)
            .Build();

        // Act
        var names = schema.GetColumnNames();

        // Assert
        Assert.Equal(2, names.Count);
        Assert.Equal("Id", names[0]);
        Assert.Equal("Name", names[1]);
    }

    [Fact]
    public void Schema_GetColumnTypes_ReturnsTypes()
    {
        // Arrange
        var schema = ParseZero.Schema.Schema.For<TestRecord>()
            .Column(t => t.Id)
            .Column(t => t.Name)
            .Column(t => t.Price)
            .Build();

        // Act
        var types = schema.GetColumnTypes();

        // Assert
        Assert.Equal(3, types.Count);
        Assert.Equal(typeof(int), types[0]);
        Assert.Equal(typeof(string), types[1]);
        Assert.Equal(typeof(decimal), types[2]);
    }

    [Fact]
    public void Schema_WithCustomColumnName_MapsCorrectly()
    {
        // Arrange
        var csv = "product_id,product_name\n1,Widget";
        var schema = ParseZero.Schema.Schema.For<TestRecord>()
            .Column(t => t.Id, "product_id")
            .Column(t => t.Name, "product_name")
            .Build();

        // Act
        TestRecord? record = null;
        bool headerRead = false;
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            if (!headerRead)
            {
                schema.MapHeader(row);
                headerRead = true;
                continue;
            }
            record = schema.Map(row);
        }

        // Assert
        Assert.NotNull(record);
        Assert.Equal(1, record.Id);
        Assert.Equal("Widget", record.Name);
    }

    [Fact]
    public void Schema_WithDateFormat_ParsesCorrectly()
    {
        // Arrange
        var csv = "Date\n15/01/2024";
        var schema = ParseZero.Schema.Schema.For<TestRecord>()
            .Column(t => t.Date, format: "dd/MM/yyyy")
            .Build();

        // Act
        TestRecord? record = null;
        bool headerRead = false;
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            if (!headerRead)
            {
                schema.MapHeader(row);
                headerRead = true;
                continue;
            }
            record = schema.Map(row);
        }

        // Assert
        Assert.NotNull(record);
        Assert.Equal(new DateTime(2024, 1, 15), record.Date);
    }

    [Fact]
    public void Schema_ImplicitConversion_Works()
    {
        // Arrange
        CsvSchema<TestRecord> schema = ParseZero.Schema.Schema.For<TestRecord>()
            .Column(t => t.Id)
            .Column(t => t.Name);

        // Assert
        Assert.NotNull(schema);
        Assert.Equal(2, schema.ColumnCount);
    }

    public class NullableRecord
    {
        public int? NullableInt { get; set; }
        public decimal? NullableDecimal { get; set; }
        public DateTime? NullableDate { get; set; }
    }

    [Fact]
    public void Schema_NullableTypes_HandlesEmptyValues()
    {
        // Arrange
        var csv = "NullableInt,NullableDecimal,NullableDate\n,,";
        var schema = ParseZero.Schema.Schema.For<NullableRecord>()
            .Column(t => t.NullableInt)
            .Column(t => t.NullableDecimal)
            .Column(t => t.NullableDate)
            .Build();

        // Act
        NullableRecord? record = null;
        bool headerRead = false;
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            if (!headerRead)
            {
                schema.MapHeader(row);
                headerRead = true;
                continue;
            }
            record = schema.Map(row);
        }

        // Assert
        Assert.NotNull(record);
        Assert.Null(record.NullableInt);
        Assert.Null(record.NullableDecimal);
        Assert.Null(record.NullableDate);
    }

    [Fact]
    public void Schema_NullableTypes_ParsesValues()
    {
        // Arrange
        var csv = "NullableInt,NullableDecimal,NullableDate\n42,123.45,2024-01-15";
        var schema = ParseZero.Schema.Schema.For<NullableRecord>()
            .Column(t => t.NullableInt)
            .Column(t => t.NullableDecimal)
            .Column(t => t.NullableDate)
            .Build();

        // Act
        NullableRecord? record = null;
        bool headerRead = false;
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            if (!headerRead)
            {
                schema.MapHeader(row);
                headerRead = true;
                continue;
            }
            record = schema.Map(row);
        }

        // Assert
        Assert.NotNull(record);
        Assert.Equal(42, record.NullableInt);
        Assert.Equal(123.45m, record.NullableDecimal);
        Assert.Equal(new DateTime(2024, 1, 15), record.NullableDate);
    }

    [Fact]
    public void Schema_MultipleRecords_MapsAll()
    {
        // Arrange
        var csv = "Id,Name\n1,Alice\n2,Bob\n3,Charlie";
        var schema = ParseZero.Schema.Schema.For<TestRecord>()
            .Column(t => t.Id)
            .Column(t => t.Name)
            .Build();

        // Act
        var records = new List<TestRecord>();
        bool headerRead = false;
        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            if (!headerRead)
            {
                schema.MapHeader(row);
                headerRead = true;
                continue;
            }
            records.Add(schema.Map(row));
        }

        // Assert
        Assert.Equal(3, records.Count);
        Assert.Equal("Alice", records[0].Name);
        Assert.Equal("Bob", records[1].Name);
        Assert.Equal("Charlie", records[2].Name);
    }
}
