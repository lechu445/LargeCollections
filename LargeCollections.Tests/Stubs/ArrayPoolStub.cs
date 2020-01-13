using System.Buffers;
using System.Collections.Generic;

namespace LargeCollections.Tests.Stubs
{
  public class ArrayPoolStub<T> : ArrayPool<T>
  {
    private readonly List<T[]> arrays;
    private int currentIdx = 0;

    public ArrayPoolStub(ICollection<int> sizes)
    {
      this.arrays = new List<T[]>(sizes.Count);
      foreach (var size in sizes)
      {
        arrays.Add(new T[size]);
      }
    }

    public override T[] Rent(int minimumLength)
    {
      return this.arrays[currentIdx++];
    }

    public override void Return(T[] array, bool clearArray = false)
    {
      var idx = this.arrays.IndexOf(array);
      this.arrays[idx] = null;
    }

    public bool AreAllReturned()
    {
      return arrays.TrueForAll(arr => arr == null);
    }
  }
}
