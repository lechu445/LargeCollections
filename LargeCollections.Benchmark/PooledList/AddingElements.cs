using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace LargeCollections.Benchmark.PooledList
{
  [MemoryDiagnoser]
  public class AddingElements
  {
    private PooledList<string> pooledList;
    private List<string> list;
    private string[] data;

    [Params(50, 500, 5_000, 50_000, 500_000, 5_000_000)]
    public int N;

    [GlobalSetup]
    public void GlobalSetup()
    {
      data = Enumerable.Range(0, this.N).Select(x => x.ToString()).ToArray();
      this.list = new List<string>();
      this.pooledList = new PooledList<string>();
    }

    [IterationSetup(Target = nameof(NormalList))]
    public void IterationSetup1() => this.list = new List<string>();


    [IterationSetup(Target = nameof(PooledList))]
    public void IterationSetup2() => this.pooledList = new PooledList<string>();

    [Benchmark(Baseline = true)]
    public void NormalList()
    {
      for (int i = 0; i < this.data.Length; i++)
      {
        this.list.Add(this.data[i]);
      }
    }

    [Benchmark]
    public void PooledList()
    {
      for (int i = 0; i < this.data.Length; i++)
      {
        this.pooledList.Add(this.data[i]);
      }
      this.pooledList.Dispose();
    }
  }
}
