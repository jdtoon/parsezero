using System.Text;
using ParseZero;

namespace ParseZero.Samples;

/// <summary>
/// Sample demonstrating encoding detection and handling.
/// Shows how ParseZero auto-detects BOMs and handles different encodings.
/// </summary>
public class EncodingDetectionSample
{
    public static void Run()
    {
        Console.WriteLine("Encoding Detection & Handling");
        Console.WriteLine("=============================\n");

        // Sample 1: UTF-8 with BOM
        Console.WriteLine("1. UTF-8 with BOM detection:");
        var utf8Content = "Name,Value\nCafé,100\nJosé,200";
        var utf8Bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(utf8Content))
            .ToArray();

        using (var stream = new MemoryStream(utf8Bytes))
        {
            foreach (var row in CsvReader.Read(stream))
            {
                Console.WriteLine($"   {row[0].ToString()}: {row[1].ToString()}");
            }
        }

        // Sample 2: UTF-16 LE with BOM
        Console.WriteLine("\n2. UTF-16 LE with BOM detection:");
        var utf16Content = "Word,Meaning\nCafé,beverage\nJour,day";
        var utf16Bytes = System.Text.Encoding.Unicode.GetPreamble()
            .Concat(System.Text.Encoding.Unicode.GetBytes(utf16Content))
            .ToArray();

        using (var stream = new MemoryStream(utf16Bytes))
        {
            foreach (var row in CsvReader.Read(stream))
            {
                Console.WriteLine($"   {row[0].ToString()}: {row[1].ToString()}");
            }
        }

        // Sample 3: Explicitly set encoding
        Console.WriteLine("\n3. Explicit encoding specification:");
        var ascii = """
            Item,Count
            A,10
            B,20
            """;

        var options = new CsvOptions
        {
            Encoding = System.Text.Encoding.ASCII,
            HasHeader = true
        };

        foreach (var row in CsvReader.Parse(ascii, options))
        {
            Console.WriteLine($"   {row[0].ToString()}: {row[1].ToString()}");
        }

        // Sample 4: Comparing parsed values with different encodings
        Console.WriteLine("\n4. Encoding impact (UTF-8 vs explicit handling):");

        // UTF-8 (default)
        Console.WriteLine("   UTF-8 parsing:");
        var utf8String = "Emoji,Count\n😀,100\n🎉,200";
        foreach (var row in CsvReader.Parse(utf8String))
        {
            var emoji = row[0].ToString();
            var count = row[1].ParseInt32();
            Console.WriteLine($"     {emoji}: {count}");
        }

        Console.WriteLine("\n   Note: ParseZero auto-detects encoding from BOM or uses specified encoding.");
        Console.WriteLine("   Supported: UTF-8, UTF-16 LE/BE, ASCII, and via EncodingProvider: ISO-8859-1, Windows-1252");
    }
}
