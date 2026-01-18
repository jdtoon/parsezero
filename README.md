# ParseZero

**The High-Performance Zero-Allocation CSV Parser for .NET**

[![NuGet](https://img.shields.io/nuget/v/ParseZero.svg)](https://www.nuget.org/packages/ParseZero)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

ParseZero is a specialized CSV parsing library focusing on **zero allocation**. It uses `Span<T>` and `Memory<T>` to read data without creating strings until absolutely necessary, keeping GC pressure near zero.

## Features

- 🚀 **Zero Allocation** - Uses `Span<T>`, `Memory<T>`, and `ArrayPool<T>` to minimize heap allocations
- ⚡ **SIMD Acceleration** - Hardware-accelerated delimiter scanning with AVX2/SSE2 on .NET 8+
- 📊 **IDataReader Support** - Plug directly into `SqlBulkCopy.WriteToServer()` for blazing-fast database inserts
- 🔄 **Streaming** - Process files of any size using `System.IO.Pipelines`
- 🎯 **Multi-Targeted** - Supports both .NET 8+ and .NET Framework 4.7.2+ (via .NET Standard 2.0)
- 🛡️ **Robust** - Handles quoted fields, escaped quotes, BOM, and mixed line endings

## Installation

```bash
dotnet add package ParseZero
```

## Quick Start

### Basic Usage

```csharp
using ParseZero;

// Simple row-by-row parsing
await foreach (var row in CsvReader.ReadAsync("data.csv"))
{
    int id = row[0].ParseInt32();
    string name = row[1].ToString();
    decimal price = row[2].ParseDecimal();
}
```

### With Schema Mapping

```csharp
using ParseZero;
using ParseZero.Schema;

public record Trade(int Id, string Symbol, decimal Price, DateTime Timestamp);

var schema = Schema.For<Trade>()
    .Column(t => t.Id)
    .Column(t => t.Symbol)
    .Column(t => t.Price, format: "C2")
    .Column(t => t.Timestamp, format: "yyyy-MM-dd HH:mm:ss");

await foreach (var trade in CsvReader.ReadAsync<Trade>("trades.csv", schema))
{
    Console.WriteLine($"{trade.Symbol}: {trade.Price}");
}
```

### SqlBulkCopy Integration

```csharp
using ParseZero;
using ParseZero.Data;
using Microsoft.Data.SqlClient;

await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();

await using var reader = CsvDataReader.Create("large-dataset.csv", options);

using var bulkCopy = new SqlBulkCopy(connection);
bulkCopy.DestinationTableName = "Trades";
await bulkCopy.WriteToServerAsync(reader);
```

### Configuration Options

```csharp
var options = new CsvOptions
{
    Delimiter = ',',
    HasHeader = true,
    Encoding = Encoding.UTF8,
    BufferSize = 4096,
    MaxLineLength = 64 * 1024,  // DoS protection
    TrimFields = true,
    AllowQuotedFields = true
};

await foreach (var row in CsvReader.ReadAsync("data.csv", options))
{
    // Process rows
}
```

## Performance

ParseZero achieves zero allocation by:

1. **Buffer Pooling** - Reuses byte arrays from `ArrayPool<T>`
2. **Span Slicing** - Returns `ReadOnlySpan<char>` slices instead of allocating strings
3. **SIMD Scanning** - Uses AVX2/SSE2 to scan 32 bytes at a time for delimiters
4. **Pipelines** - Streams data through `System.IO.Pipelines` for optimal I/O

### Benchmark Results

```
| Method              | Mean     | Allocated |
|---------------------|----------|-----------|
| ParseZero           | 45.2 ms  | 0 B       |
| CsvHelper           | 312.5 ms | 1.2 GB    |
| Sylvan.Data.Csv     | 52.1 ms  | 48 KB     |
```

*Benchmark: 1M rows, 10 columns, .NET 8, Release build*

## Supported Encodings

- UTF-8 (with and without BOM)
- UTF-16 LE/BE (with BOM detection)
- ISO-8859-1 (Latin-1)
- Windows-1252

## Target Frameworks

| Framework | SIMD Support |
|-----------|--------------|
| .NET 8.0+ | ✅ AVX2/SSE2 |
| .NET Standard 2.0 | ❌ Scalar only |
| .NET Framework 4.7.2+ | ❌ Scalar only |

## License

MIT License - see [LICENSE](LICENSE) for details.

## Contributing

Contributions are welcome! Please read our [Contributing Guide](CONTRIBUTING.md) for details.
