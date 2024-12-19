using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Utility.Collections
{
    public class HashList<T> : IList<T>
    {
        private readonly List<T> _list = new();
        private readonly Dictionary<T, int> _lookup = new();
        private int _loopIndex = -1;

        public int Count => _list.Count;

        public void Add(T item)
        {
            if (item == null || _lookup.ContainsKey(item))
                return;

            _lookup.Add(item, _list.Count);
            _list.Add(item);
        }

        public void Remove(T item)
        {
            if (!_lookup.Remove(item, out var loopIndex))
                return;

            if (loopIndex == _loopIndex)
                --_loopIndex;
            else if (loopIndex < _loopIndex)
            {
                _list.Swap<T>(_loopIndex, loopIndex, out var newDestinationValue);
                _lookup[newDestinationValue] = loopIndex;
                loopIndex = _loopIndex;
                --_loopIndex;
            }

            _list.RemoveAtSwapBack<T>(loopIndex, out var swappedItem);
            if (swappedItem == null)
                return;
            _lookup[swappedItem] = loopIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _lookup.Clear();
            _list.Clear();
        }

        public bool IsReadOnly => false;

        T IList<T>.this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => index <= _list.Count - 1 && index >= 0 ? _list[index] : default(T);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (index >= _list.Count || index <= -1)
                    return;
                _list[index] = value;
            }
        }

        public T this[int index] => index <= _list.Count && index >= 0 ? _list[index] : default(T);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsItem(T item) => _lookup.ContainsKey(item);

        public Enumerator GetEnumerator() => new(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(T item)
        {
            return _lookup.TryGetValue(item, out var num) ? num : -1;
        }

        public void Insert(int index, T item)
        {
        }

        public void RemoveAt(int index)
        {
            if (index > _list.Count - 1)
                return;
            _lookup.Remove(_list[index]);
            _list.RemoveAt(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T item) => _lookup.ContainsKey(item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        bool ICollection<T>.Remove(T item)
        {
            if (!_lookup.Remove(item, out var index))
                return false;
            _list.RemoveAt(index);
            return true;
        }

        public readonly struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            private readonly HashList<T> _list;

            public Enumerator(HashList<T> list)
            {
                _list = list;
                Reset();
            }

            public T Current => _list[_list._loopIndex];

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                Reset();
            }

            public bool MoveNext()
            {
                if (_list._loopIndex >= _list.Count - 1)
                    return false;

                ++_list._loopIndex;
                return true;
            }

            public void Reset() => _list._loopIndex = -1;
        }
    }
}