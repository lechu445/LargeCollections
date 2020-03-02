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
    private readonly int _bucketSize;
    private readonly List<T[]> _buckets;
    private readonly ArrayPool<T> _pool = ArrayPool<T>.Shared;
    private int _count;
    private int _version;

    public PooledList(int bucketSize = 5000)
    {
      _buckets = new List<T[]>();
      _bucketSize = bucketSize;
    }

    public PooledList(int initialCapacity, int bucketSize = 5000)
    {
      int bucketCount = GetBucketIndex(initialCapacity) + 1;
      _buckets = new List<T[]>(bucketCount);
      _bucketSize = bucketSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetBucketIndex(int index)
    {
      return index / _bucketSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetIndexInBucket(int index)
    {
      return index % _bucketSize;
    }

    private void AddBucket()
    {
      _buckets.Add(_pool.Rent(minimumLength: _bucketSize));
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
        var len = GetBucketLength(i);
        Array.Clear(_buckets[i], 0, len);
      }
      _version++;
    }

    public bool Contains(T item)
    {
      for (int i = 0; i < _buckets.Count; i++)
      {
        var len = GetBucketLength(i);
        if (Array.IndexOf(_buckets[i], item, 0, len) >= 0)
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
        var len = GetBucketLength(i);
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

    private int GetBucketLength(int i)
    {
      return i == _buckets.Count - 1 ? GetIndexInBucket(_count - 1) + 1 : _bucketSize;
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
