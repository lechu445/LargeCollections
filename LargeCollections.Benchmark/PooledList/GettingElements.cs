using BenchmarkDotNet.Attributes;
using System.Collections.Generic;

namespace LargeCollections.Benchmark.PooledList
{
  [MemoryDiagnoser]
  public class GettingElements
  {
    private PooledList<int> pooledList;
    private List<int> list;

    [Params(50, 500, 5_000, 50_000, 500_000, 5_000_000)]
    public int N;

    [GlobalSetup]
    public void GlobalSetup()
    {
      this.list = new List<int>();
      this.pooledList = new PooledList<int>();
      for (int i = 0; i < this.N; i++)
      {
        this.list.Add(i);
        this.pooledList.Add(i);
      }
    }

    [Benchmark(Baseline = true)]
    public int NormalList()
    {
      var sum = 0;
      for (int i = 0; i < this.N; i++)
      {
        sum += this.list[i];
      }
      return sum;
    }

    [Benchmark]
    public int PooledList()
    {
      var sum = 0;
      for (int i = 0; i < this.N; i++)
      {
        sum += this.pooledList[i];
      }
      return sum;
    }

    [GlobalCleanup]
    public void Clean() => this.pooledList.Dispose();
  }
}
