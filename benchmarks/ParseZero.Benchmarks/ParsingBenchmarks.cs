using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CsvHelper;
using CsvHelper.Configuration;
using SylvanCsv = Sylvan.Data.Csv;

namespace ParseZero.Benchmarks;

/// <summary>
/// Benchmarks comparing ParseZero against CsvHelper and Sylvan.Data.Csv
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[MarkdownExporterAttribute.GitHub]
public class ParsingBenchmarks
{
    private string _testFilePath = null!;
    private byte[] _testData = null!;

    [Params(1000, 10000, 100000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Generate test data
        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,Price,Quantity,Date,IsActive,Category,Description");

        var random = new Random(42); // Fixed seed for reproducibility
        var categories = new[] { "Electronics", "Clothing", "Food", "Books", "Home" };

        for (int i = 0; i < RowCount; i++)
        {
            sb.AppendLine($"{i},{GenerateName(random)},{random.Next(1, 10000)}.{random.Next(0, 99):D2},{random.Next(1, 1000)},{DateTime.Now.AddDays(-random.Next(0, 365)):yyyy-MM-dd},{(random.Next(2) == 1 ? "true" : "false")},{categories[random.Next(categories.Length)]},\"A description for item {i}\"");
        }

        _testData = System.Text.Encoding.UTF8.GetBytes(sb.ToString());

        // Write to temp file for file-based benchmarks
        _testFilePath = Path.GetTempFileName();
        File.WriteAllBytes(_testFilePath, _testData);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    private static string GenerateName(Random random)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var length = random.Next(5, 15);
        var name = new char[length];
        for (int i = 0; i < length; i++)
        {
            name[i] = chars[random.Next(chars.Length)];
        }
        return new string(name);
    }

    #region ParseZero Benchmarks

    [Benchmark(Baseline = true)]
    public int ParseZero_Stream()
    {
        int count = 0;
        using var stream = new MemoryStream(_testData);

        foreach (var row in CsvReader.Read(stream))
        {
            // Access all fields to ensure parsing happens
            _ = row[0].ParseInt32();
            _ = row[1].ToString();
            _ = row[2].ParseDecimal();
            _ = row[3].ParseInt32();
            _ = row[4].ParseDateTime();
            _ = row[5].ParseBoolean();
            _ = row[6].ToString();
            _ = row[7].ToString();
            count++;
        }

        return count;
    }

    [Benchmark]
    public int ParseZero_FieldCountOnly()
    {
        int count = 0;
        using var stream = new MemoryStream(_testData);

        foreach (var row in CsvReader.Read(stream))
        {
            // Just count fields without parsing values
            count += row.FieldCount;
        }

        return count;
    }

    [Benchmark]
    public int ParseZero_File()
    {
        int count = 0;

        foreach (var row in CsvReader.Read(_testFilePath))
        {
            _ = row[0].ParseInt32();
            _ = row[1].ToString();
            count++;
        }

        return count;
    }

    [Benchmark]
    public int ParseZero_ForEach()
    {
        int count = 0;

        CsvReader.ForEach(_testFilePath, (in ParseZero.Core.CsvRow row) =>
        {
            _ = row[0].ParseInt32();
            _ = row[1].ToString();
            count++;
            return true;
        });

        return count;
    }

    #endregion

    #region CsvHelper Benchmarks

    [Benchmark]
    public int CsvHelper_Stream()
    {
        int count = 0;
        using var stream = new MemoryStream(_testData);
        using var reader = new StreamReader(stream);
        using var csv = new CsvHelper.CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });

        while (csv.Read())
        {
            _ = csv.GetField<int>(0);
            _ = csv.GetField<string>(1);
            _ = csv.GetField<decimal>(2);
            _ = csv.GetField<int>(3);
            _ = csv.GetField<DateTime>(4);
            _ = csv.GetField<bool>(5);
            _ = csv.GetField<string>(6);
            _ = csv.GetField<string>(7);
            count++;
        }

        return count;
    }

    [Benchmark]
    public int CsvHelper_File()
    {
        int count = 0;
        using var reader = new StreamReader(_testFilePath);
        using var csv = new CsvHelper.CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });

        while (csv.Read())
        {
            _ = csv.GetField<int>(0);
            _ = csv.GetField<string>(1);
            count++;
        }

        return count;
    }

    #endregion

    #region Sylvan Benchmarks

    [Benchmark]
    public int Sylvan_Stream()
    {
        int count = 0;
        using var stream = new MemoryStream(_testData);
        using var reader = new StreamReader(stream);
        using var csv = SylvanCsv.CsvDataReader.Create(reader);

        while (csv.Read())
        {
            _ = csv.GetInt32(0);
            _ = csv.GetString(1);
            _ = csv.GetDecimal(2);
            _ = csv.GetInt32(3);
            _ = csv.GetDateTime(4);
            _ = csv.GetBoolean(5);
            _ = csv.GetString(6);
            _ = csv.GetString(7);
            count++;
        }

        return count;
    }

    [Benchmark]
    public int Sylvan_File()
    {
        int count = 0;
        using var reader = new StreamReader(_testFilePath);
        using var csv = SylvanCsv.CsvDataReader.Create(reader);

        while (csv.Read())
        {
            _ = csv.GetInt32(0);
            _ = csv.GetString(1);
            count++;
        }

        return count;
    }

    #endregion
}
