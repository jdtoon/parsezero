using BenchmarkDotNet.Running;

namespace ParseZero.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        // Run all benchmarks
        var summary = BenchmarkRunner.Run(typeof(Program).Assembly, args: args);
    }
}
