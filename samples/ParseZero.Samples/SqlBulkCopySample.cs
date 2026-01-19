using System.Data;
using ParseZero;
using ParseZero.Data;

namespace ParseZero.Samples;

/// <summary>
/// Sample demonstrating end-to-end usage with IDataReader and SqlBulkCopy pattern.
/// Note: This is a mock example. For a real database, use Microsoft.Data.SqlClient.
/// </summary>
public class SqlBulkCopySample
{
    public static void Run()
    {
        Console.WriteLine("SqlBulkCopy Integration (IDataReader Pattern)");
        Console.WriteLine("=============================================\n");

        // Generate sample CSV
        var csv = GenerateSampleCsv(rowCount: 100);

        // Example 1: Using CsvDataReader with mock IDataReader consumer
        Console.WriteLine("1. Using CsvDataReader as IDataReader:");
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
        using var reader = CsvDataReader.Create(stream, new CsvOptions { HasHeader = true });

        Console.WriteLine($"   Schema: {string.Join(", ", Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i)))}");
        Console.WriteLine($"   Rows loaded:");

        int recordCount = 0;
        decimal totalAmount = 0;

        while (reader.Read())
        {
            recordCount++;
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var amount = reader.GetDecimal(2);
            totalAmount += amount;

            if (recordCount <= 3)
            {
                Console.WriteLine($"     {id}: {name} - {amount:C}");
            }
        }

        Console.WriteLine($"     ... ({recordCount - 3} more rows)");
        Console.WriteLine($"   Total: {recordCount} records, Sum: {totalAmount:C}");

        // Example 2: Field schema inspection
        Console.WriteLine("\n2. Schema inspection for bulk copy mapping:");
        using var stream2 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));
        using var reader2 = CsvDataReader.Create(stream2, new CsvOptions { HasHeader = true });

        var schema = reader2.GetSchemaTable();
        foreach (DataRow column in schema!.Rows)
        {
            var name = column["ColumnName"];
            var ordinal = column["ColumnOrdinal"];
            var allowNull = column["AllowDBNull"];
            Console.WriteLine($"     [{ordinal}] {name} (nullable: {allowNull})");
        }

        // Example 3: Nested CSV with quoted fields
        Console.WriteLine("\n3. Handling complex CSV with quoted fields:");
        var complexCsv = """
            Id,Name,Notes
            1,"Smith, John","Important: Handle with care"
            2,"Johnson, Jane","Standard item"
            3,"Lee, Michael","Contains ""quotes"" in text"
            """;

        using var stream3 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(complexCsv));
        using var reader3 = CsvDataReader.Create(stream3, new CsvOptions { HasHeader = true });

        while (reader3.Read())
        {
            Console.WriteLine($"     ID: {reader3.GetInt32(0)}, Name: {reader3.GetString(1)}, Notes: {reader3.GetString(2)}");
        }

        // Example 4: Real-world SqlBulkCopy usage pattern
        Console.WriteLine("\n4. Real-world SqlBulkCopy pattern (pseudo-code):");
        Console.WriteLine("""
               using (var connection = new SqlConnection(connectionString))
               {
                   connection.Open();

                   using (var reader = CsvDataReader.Create("data.csv", options))
                   {
                       using (var bulkCopy = new SqlBulkCopy(connection))
                       {
                           bulkCopy.DestinationTableName = "TargetTable";
                           bulkCopy.BatchSize = 5000;
                           bulkCopy.BulkCopyTimeout = 300;

                           // Map CSV columns to database columns if names differ
                           // bulkCopy.ColumnMappings.Add("CsvName", "DbName");

                           bulkCopy.WriteToServer(reader);
                       }
                   }
               }
            """);

        Console.WriteLine("\nKey advantages:");
        Console.WriteLine("  • Zero-allocation CSV parsing minimizes GC pressure during bulk load");
        Console.WriteLine("  • IDataReader interface enables direct SqlBulkCopy integration");
        Console.WriteLine("  • Memory-efficient streaming for files > 1GB");
    }

    private static string GenerateSampleCsv(int rowCount)
    {
        var lines = new List<string> { "OrderId,CustomerName,OrderAmount" };
        var random = new Random(42);

        for (int i = 1; i <= rowCount; i++)
        {
            var orderId = 10000 + i;
            var name = $"Customer_{i}";
            var amount = random.Next(100, 10000) + random.NextDouble();
            lines.Add($"{orderId},{name},{amount:F2}");
        }

        return string.Join("\n", lines);
    }
}
