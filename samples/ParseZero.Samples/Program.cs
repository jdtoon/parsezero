using System.Text;
using ParseZero;
using ParseZero.Data;
using ParseZero.Schema;

namespace ParseZero.Samples;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("ParseZero Samples");
        Console.WriteLine("=================\n");

        BasicParsing();
        TypedParsing();
        SchemaMapping();
        DataReaderExample();

        Console.WriteLine("\nAll samples completed!");
    }

    /// <summary>
    /// Basic CSV parsing with zero allocations
    /// </summary>
    private static void BasicParsing()
    {
        Console.WriteLine("1. Basic Parsing");
        Console.WriteLine("----------------");

        var csv = """
            Name,Age,City
            Alice,30,New York
            Bob,25,Los Angeles
            Charlie,35,Chicago
            """;

        foreach (var row in CsvReader.Parse(csv))
        {
            // Zero-allocation field access via Span<char>
            var name = row[0].ToString();
            var age = row[1].ToString();
            var city = row[2].ToString();
            Console.WriteLine($"  {name} is {age} years old from {city}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Parsing with typed field access
    /// </summary>
    private static void TypedParsing()
    {
        Console.WriteLine("2. Typed Parsing");
        Console.WriteLine("----------------");

        var csv = """
            Id,Price,Quantity,Date,IsActive
            1,99.99,10,2024-01-15,true
            2,149.50,5,2024-02-20,false
            3,299.00,3,2024-03-10,true
            """;

        decimal totalRevenue = 0;
        foreach (var row in CsvReader.Parse(csv))
        {
            var id = row[0].ParseInt32();
            var price = row[1].ParseDecimal();
            var quantity = row[2].ParseInt32();
            var date = row[3].ParseDateTime();
            var isActive = row[4].ParseBoolean();

            if (isActive)
            {
                totalRevenue += price * quantity;
            }

            Console.WriteLine($"  ID: {id}, Price: {price:C}, Qty: {quantity}, Date: {date:d}, Active: {isActive}");
        }

        Console.WriteLine($"  Total revenue from active items: {totalRevenue:C}");
        Console.WriteLine();
    }

    /// <summary>
    /// Schema-based mapping to strongly typed objects
    /// </summary>
    private static void SchemaMapping()
    {
        Console.WriteLine("3. Schema Mapping");
        Console.WriteLine("-----------------");

        var csv = """
            ProductId,ProductName,UnitPrice,UnitsInStock
            1,Widget,19.99,100
            2,Gadget,49.99,50
            3,Gizmo,9.99,200
            """;

        var schema = Schema.Schema.For<Product>()
            .Column(p => p.Id, "ProductId")
            .Column(p => p.Name, "ProductName")
            .Column(p => p.Price, "UnitPrice")
            .Column(p => p.Stock, "UnitsInStock")
            .Build();

        var products = new List<Product>();
        bool headerRead = false;

        foreach (var row in CsvReader.Parse(csv, new CsvOptions { HasHeader = false }))
        {
            if (!headerRead)
            {
                schema.MapHeader(row);
                headerRead = true;
                continue;
            }
            var product = schema.Map(row);
            products.Add(product);
            Console.WriteLine($"  {product.Name}: ${product.Price} ({product.Stock} in stock)");
        }

        Console.WriteLine($"  Total products: {products.Count}");
        Console.WriteLine();
    }

    /// <summary>
    /// Using CsvDataReader for IDataReader compatibility
    /// </summary>
    private static void DataReaderExample()
    {
        Console.WriteLine("4. IDataReader (SqlBulkCopy compatible)");
        Console.WriteLine("---------------------------------------");

        var csv = """
            Id,Name,Value
            1,Alpha,100.5
            2,Beta,200.75
            3,Gamma,300.25
            """;

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
        using var reader = new CsvDataReader(stream);

        Console.WriteLine($"  Field count: {reader.FieldCount}");

        for (int i = 0; i < reader.FieldCount; i++)
        {
            Console.WriteLine($"    Column {i}: {reader.GetName(i)}");
        }

        Console.WriteLine("\n  Data:");
        while (reader.Read())
        {
            Console.WriteLine($"    {reader.GetInt32(0)}, {reader.GetString(1)}, {reader.GetDecimal(2)}");
        }

        // Example of how to use with SqlBulkCopy (commented out as it requires a real connection)
        /*
        using var connection = new SqlConnection("your-connection-string");
        connection.Open();

        using var bulkCopy = new SqlBulkCopy(connection);
        bulkCopy.DestinationTableName = "Products";
        bulkCopy.WriteToServer(reader);
        */

        Console.WriteLine("\n  Note: CsvDataReader is compatible with SqlBulkCopy.WriteToServer()");
    }
}

/// <summary>
/// Sample product class for schema mapping
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
