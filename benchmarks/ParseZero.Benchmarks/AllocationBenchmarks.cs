using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace ParseZero.Benchmarks;

/// <summary>
/// Benchmarks for memory allocation comparison
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[MarkdownExporterAttribute.GitHub]
public class AllocationBenchmarks
{
    private byte[] _smallData = null!;
    private byte[] _mediumData = null!;
    private byte[] _largeData = null!;

    [GlobalSetup]
    public void Setup()
    {
        _smallData = GenerateTestData(100);
        _mediumData = GenerateTestData(10000);
        _largeData = GenerateTestData(100000);
    }

    private static byte[] GenerateTestData(int rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,Value");

        for (int i = 0; i < rows; i++)
        {
            sb.AppendLine($"{i},Item{i},{i * 1.5m}");
        }

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    [Benchmark]
    public long ParseZero_SmallFile_SumOnly()
    {
        long sum = 0;
        using var stream = new MemoryStream(_smallData);

        foreach (var row in CsvReader.Read(stream))
        {
            sum += row[0].ParseInt64();
        }

        return sum;
    }

    [Benchmark]
    public long ParseZero_MediumFile_SumOnly()
    {
        long sum = 0;
        using var stream = new MemoryStream(_mediumData);

        foreach (var row in CsvReader.Read(stream))
        {
            sum += row[0].ParseInt64();
        }

        return sum;
    }

    [Benchmark]
    public long ParseZero_LargeFile_SumOnly()
    {
        long sum = 0;
        using var stream = new MemoryStream(_largeData);

        foreach (var row in CsvReader.Read(stream))
        {
            sum += row[0].ParseInt64();
        }

        return sum;
    }

    [Benchmark]
    public int ParseZero_SmallFile_ToString()
    {
        int count = 0;
        using var stream = new MemoryStream(_smallData);

        foreach (var row in CsvReader.Read(stream))
        {
            // This will allocate strings
            _ = row[1].ToString();
            count++;
        }

        return count;
    }

    [Benchmark]
    public int ParseZero_MediumFile_ToString()
    {
        int count = 0;
        using var stream = new MemoryStream(_mediumData);

        foreach (var row in CsvReader.Read(stream))
        {
            // This will allocate strings
            _ = row[1].ToString();
            count++;
        }

        return count;
    }

    [Benchmark]
    public int ParseZero_LargeFile_ToString()
    {
        int count = 0;
        using var stream = new MemoryStream(_largeData);

        foreach (var row in CsvReader.Read(stream))
        {
            // This will allocate strings
            _ = row[1].ToString();
            count++;
        }

        return count;
    }

    [Benchmark]
    public int ParseZero_ZeroAlloc_SpanAccess()
    {
        int count = 0;
        using var stream = new MemoryStream(_mediumData);

        foreach (var row in CsvReader.Read(stream))
        {
            // Pure span access - no allocations
            var span = row[1].Span;
            count += span.Length;
        }

        return count;
    }

    [Benchmark]
    public int ParseZero_IntParsing_NoAlloc()
    {
        int count = 0;
        using var stream = new MemoryStream(_mediumData);

        foreach (var row in CsvReader.Read(stream))
        {
            // Int parsing should be zero-allocation
            count += row[0].ParseInt32();
        }

        return count;
    }
}
