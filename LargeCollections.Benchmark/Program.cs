using BenchmarkDotNet.Running;
using System;

namespace LargeCollections.Benchmark
{
  public class Program
  {
    static void Main(string[] args)
    {
      BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
  }
}
