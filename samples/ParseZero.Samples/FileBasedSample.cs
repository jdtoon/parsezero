using System.Text;
using ParseZero;
using ParseZero.Core;

namespace ParseZero.Samples;

/// <summary>
/// Sample demonstrating file-based CSV parsing with streaming.
/// </summary>
public class FileBasedSample
{
    public static void Run()
    {
        Console.WriteLine("File-Based CSV Parsing");
        Console.WriteLine("=====================\n");

        // Create a sample CSV file
        string filePath = Path.Combine(Path.GetTempPath(), "sample_large.csv");
        CreateSampleFile(filePath, 1000);

        try
        {
            // Method 1: File-based parsing with CsvReader.Read
            Console.WriteLine("1. File-based parsing:");
            var startTime = DateTime.UtcNow;
            int rowCount = 0;
            decimal sumValues = 0;

            // Parse file using CsvReader.Read which returns CsvFileReader
            var fileReader = CsvReader.Read(filePath);
            foreach (var row in fileReader)
            {
                rowCount++;
                if (row.FieldCount >= 3)
                {
                    sumValues += row[2].ParseDecimal();
                }
            }

            var elapsed = DateTime.UtcNow - startTime;
            Console.WriteLine($"   Processed {rowCount} rows in {elapsed.TotalMilliseconds:F2}ms");
            Console.WriteLine($"   Sum of column 3: {sumValues:F2}");

            // Method 2: Custom buffer size for streaming performance
            Console.WriteLine("\n2. Custom buffer size (64KB) for streaming performance:");
            var options = new CsvOptions
            {
                HasHeader = true,
                BufferSize = 64 * 1024, // 64KB buffer
                TrimFields = false
            };

            startTime = DateTime.UtcNow;
            rowCount = 0;
            sumValues = 0;

            var bufferedReader = CsvReader.Read(filePath, options);
            foreach (var row in bufferedReader)
            {
                rowCount++;
                if (row.FieldCount >= 3)
                {
                    sumValues += row[2].ParseDecimal();
                }
            }

            elapsed = DateTime.UtcNow - startTime;
            Console.WriteLine($"   Processed {rowCount} rows in {elapsed.TotalMilliseconds:F2}ms");
            Console.WriteLine($"   Sum of column 3: {sumValues:F2}");

            Console.WriteLine("\n   Benefits of file-based parsing:");
            Console.WriteLine("   • Zero-allocation field access via Span<T>");
            Console.WriteLine("   • Memory-efficient streaming for large files");
            Console.WriteLine("   • Configurable buffer sizes for throughput tuning");
        }
        finally
        {
            // Cleanup
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        Console.WriteLine();
    }

    private static void CreateSampleFile(string filePath, int rowCount)
    {
        using (var writer = File.CreateText(filePath))
        {
            // Write header
            writer.WriteLine("Id,Name,Value,Description");

            // Write data rows
            var random = new Random(42);
            for (int i = 1; i <= rowCount; i++)
            {
                var id = i;
                var name = $"Item_{i}";
                var value = random.NextDouble() * 1000;
                var description = $"Description for item {i}";

                writer.WriteLine($"{id},\"{name}\",{value:F2},\"{description}\"");
            }
        }
    }
}
