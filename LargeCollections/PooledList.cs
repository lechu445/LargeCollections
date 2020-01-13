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
    private struct Bucket
    {
      public int StartIndex;
      public int Count;

      public T[] Entries;

      public Bucket(int startIndex, T[] entries)
      {
        this.StartIndex = startIndex;
        this.Count = 0;
        this.Entries = entries;
      }

      public T GetItem(int index)
      {
        Debug.Assert(index - this.StartIndex < this.Count);
        return Entries[index - this.StartIndex];
      }

      public void SetItem(int index, T item)
      {
        Debug.Assert(index - this.StartIndex < this.Count);
        Entries[index - this.StartIndex] = item;
      }

      public void RemoveAt(int index)
      {
        var idx = index - this.StartIndex;
        Debug.Assert(idx >= 0);
        Debug.Assert(idx < this.Count);
        if (idx == this.Count - 1)
        {
          this.Count--;
        }
        else
        {
          Array.Copy(Entries, idx + 1, Entries, idx, this.Count - idx);
        }
      }

      public bool CanAdd() => this.Count < this.Entries.Length;

      public void Add(T item)
      {
        Debug.Assert(CanAdd());
        this.Entries[this.Count] = item;
        this.Count++;
      }

      public int IndexOf(T item)
      {
        var idx = Array.IndexOf(this.Entries, item);
        return (idx != -1) ? (this.StartIndex + idx) : -1;
      }

      public void Clear()
      {
        Debug.Assert(this.Count >= 0);
        this.Count = 0;
      }
    }

    private const int MaxBucketSize = 1_048_576;
    internal ArrayPool<T> _pool = ArrayPool<T>.Shared;
    private Bucket[] _buckets;
    private int _lastBucketIndex = -1;
    private int _count;
    private int _version;

    public PooledList()
    {
      _buckets = new Bucket[5];
    }

    public PooledList(int initialCapacity)
    {
      _buckets = new Bucket[5];
      AddBucket(startIdx: 0, size: initialCapacity);
    }

    private void AddBucket(int startIdx, int size)
    {
      var entries = this._pool.Rent(minimumLength: Math.Min(size, MaxBucketSize));
      var bucket = new Bucket(startIdx, entries);
      if (_lastBucketIndex == _buckets.Length - 2)
      {
        var newBuckets = new Bucket[_buckets.Length + 1];
        Array.Copy(_buckets, 0, newBuckets, 0, _buckets.Length);
        _buckets = newBuckets;
      }
      _buckets[++_lastBucketIndex] = bucket;
    }

    public T this[int index]
    {
      get => FindBucket(index).GetItem(index);
      set => FindBucket(index).SetItem(index, value);
    }

    public int Count => _count;

    public bool IsReadOnly => false;

    public void Add(T item)
    {
      _count++;
      if (_lastBucketIndex == -1)
      {
        AddBucket(startIdx: 0, size: 16);
        ref Bucket lastBucket = ref _buckets[_lastBucketIndex];
        lastBucket.Add(item);
      }
      else
      {
        ref Bucket lastBucket = ref _buckets[_lastBucketIndex];
        if (!lastBucket.CanAdd())
        {
          AddBucket(
            startIdx: lastBucket.StartIndex + lastBucket.Count,
            size: lastBucket.Count * 2);
          lastBucket = ref _buckets[_lastBucketIndex];
        }
        lastBucket.Add(item);
      }
      _version++;
    }

    public void Clear()
    {
      for (var i = 0; i <= _lastBucketIndex; i++)
      {
        _buckets[i].Clear();
      }
      _version++;
    }

    public bool Contains(T item)
    {
      return this.IndexOf(item) != -1;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
      for (var bucketIndex = 0; bucketIndex <= _lastBucketIndex; bucketIndex++)
      {
        ref Bucket bucket = ref _buckets[bucketIndex];
        var entries = bucket.Entries;
        for (int i = 0; i < _buckets[bucketIndex].Count; i++)
        {
          array[arrayIndex++] = entries[i];
        }
      }
    }

    public IEnumerator<T> GetEnumerator() => new Enumerator(this);

    public int IndexOf(T item)
    {
      for (var i = 0; i <= _lastBucketIndex; i++)
      {
        var idx = _buckets[i].IndexOf(item);
        if (idx != -1)
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
    private ref Bucket FindBucket(int index)
    {
      for (int i = 0; i <= _lastBucketIndex; i++)
      {
        if (_buckets[i].StartIndex + _buckets[i].Count > index)
        {
          Debug.Assert(_buckets[i].StartIndex >= index);
          return ref _buckets[i];
        }
      }
      throw new IndexOutOfRangeException();
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          for (var i = 0; i <= _lastBucketIndex; i++)
          {
            _buckets[i].Clear();
            _pool.Return(_buckets[i].Entries);
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
