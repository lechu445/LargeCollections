using System;
using System.Linq;
using Xunit;

namespace LargeCollections.Tests
{
  public class PooledListTest
  {
    //[Fact]
    //public void No_Initialcapacity()
    //{
    //  const int howMuch = 17;
    //  var expected = new System.Collections.Generic.List<string>(Enumerable.Range(0, howMuch).Select(x => x.ToString()));
    //  var pool = new Stubs.ArrayPoolStub<string>(new int[] { 15, 20 });
    //  var list = new PooledList<string>();
    //  list._pool = pool;
    //  for (int i = 0; i < howMuch; i++)
    //  {
    //    list.Add(expected[i]);
    //  }
    //  Assert.Equal(howMuch, list.Count);
    //  for (int i = 0; i < howMuch; i++)
    //  {
    //    Assert.Equal(expected[i], list[i]);
    //  }
    //  Assert.Equal(expected, list.ToList());
    //  list.Dispose();
    //  Assert.True(pool.AreAllReturned());
    //}

    [Fact]
    public void DefaultArrayPool()
    {
      const int howMuch = 5_000_000;
      var expected = new System.Collections.Generic.List<string>(Enumerable.Range(0, howMuch).Select(x => x.ToString()));

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
