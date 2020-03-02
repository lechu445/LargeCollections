using System;
using System.Linq;
using Xunit;

namespace LargeCollections.Tests
{
  public class PooledListTest
  {
    [Fact]
    public void Small_Buckets()
    {
      const int howMuch = 11;
      var expected = Enumerable.Range(0, howMuch).ToList();
      var list = new PooledList<int>(bucketSize: 5);
      for (int i = 0; i < howMuch; i++)
      {
        list.Add(i);
      }
      Assert.Equal(expected, list.ToList());
      list.Dispose();
    }

    [Fact]
    public void DefaultArrayPool()
    {
      const int howMuch = 5_000_000;
      var expected = Enumerable.Range(0, howMuch).Select(x => x.ToString()).ToList();

      var list = new PooledList<string>();
      for (int i = 0; i < howMuch; i++)
      {
        list.Add(expected[i]);
      }
      Assert.Equal(howMuch, list.Count);
      for (int i = 0; i < howMuch; i++)
      {
        Assert.Equal(expected[i], list[i]);
      }
      Assert.Equal(expected.Count, list.Count);
      Assert.Equal(expected, list.ToList());
      list.Dispose();
    }
  }
}
