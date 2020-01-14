using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LargeCollections
{
  public class PooledList<T> : IList<T>, IDisposable
  {
    private const int BucketSize = 5000;
    List<T[]> _buckets;
    internal ArrayPool<T> _pool = ArrayPool<T>.Shared;//.Create(maxArrayLength: BucketSize * 2, maxArraysPerBucket: BucketSize);
    private int _count;
    private int _version;

    public PooledList()
    {
      _buckets = new List<T[]>();
    }

    public PooledList(int initialCapacity)
    {
      int bucketCount = GetBucketIndex(initialCapacity) + 1;
      _buckets = new List<T[]>(bucketCount);
      //for (int i = 0; i < bucketCount; i++)
      //{
      //  AddBucket();
      //}
    }

    private int GetBucketIndex(int index)
    {
      return index / BucketSize;
    }

    private int GetIndexInBucket(int index)
    {
      return index % BucketSize;
    }

    private void AddBucket()
    {
      _buckets.Add(_pool.Rent(minimumLength: BucketSize));
    }

    public T this[int index]
    {
      get => FindBucket(index)[GetIndexInBucket(index)];
      set => FindBucket(index)[GetIndexInBucket(index)] = value;
    }

    public int Count => _count;

    public bool IsReadOnly => false;

    public void Add(T item)
    {
      if (GetIndexInBucket(_count) == 0)
      {
        AddBucket();
      }

      _buckets[_buckets.Count - 1][GetIndexInBucket(_count)] = item;
      _count++;
      _version++;
    }

    public void Clear()
    {
      for (var i = 0; i <= GetBucketIndex(_count); i++)
      {
        Array.Clear(_buckets[i], 0, _buckets[i].Length);
      }
      _version++;
    }

    public bool Contains(T item)
    {
      for (int i = 0; i < _buckets.Count; i++)
      {
        if (Array.IndexOf(_buckets[i], item) >= 0)
        {
          return true;
        }
      }
      return false;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
      Debug.Assert(arrayIndex < _count);
      for (int i = 0; i < _buckets.Count; i++)
      {
        int len = i == _buckets.Count - 1 ? GetIndexInBucket(_count - 1) + 1 : BucketSize;
        Array.Copy(_buckets[i], 0, array, arrayIndex, len);
        arrayIndex += len;
      }
    }

    public IEnumerator<T> GetEnumerator() => new Enumerator(this);

    public int IndexOf(T item)
    {
      for (int i = 0; i < _buckets.Count; i++)
      {
        var idx = Array.IndexOf(_buckets[i], item);
        if (idx >= 0)
        {
          return idx;
        }
      }
      return -1;
    }

    public void Insert(int index, T item)
    {
      throw new NotImplementedException();
    }

    public bool Remove(T item)
    {
      var idx = this.IndexOf(item);
      if (idx != -1)
      {
        RemoveAt(idx);
        return true;
      }
      else
      {
        return false;
      }
    }

    public void RemoveAt(int index)
    {
      throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T[] FindBucket(int index)
    {
      return _buckets[GetBucketIndex(index)];
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          for (var i = 0; i < _buckets.Count; i++)
          {
            _pool.Return(_buckets[i]);
          }
        }

        disposedValue = true;
      }
    }

    public void Dispose()
    {
      Dispose(true);
    }
    #endregion

    public struct Enumerator : IEnumerator<T>
    {
      private readonly PooledList<T> _list;
      private readonly int _version;
      private int _index;
      private T _current;

      internal Enumerator(PooledList<T> list)
      {
        _list = list;
        _index = 0;
        _version = list._version;
        _current = default;
      }

      public T Current => _current;

      object IEnumerator.Current => _current;

      public void Dispose()
      {
      }

      public bool MoveNext()
      {
        if (_version != _list._version)
        {
          throw new InvalidOperationException("failed version");
        }

        if ((uint)_index >= (uint)_list._count)
        {
          _index = _list._count + 1;
          _current = default;
          return false;
        }
        _current = _list[_index];
        _index++;
        return true;
      }

      public void Reset()
      {
        if (_version != _list._version)
        {
          throw new InvalidOperationException("failed version");
        }

        _index = 0;
        _current = default;
      }
    }
  }
}
