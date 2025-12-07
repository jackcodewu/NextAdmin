using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NextAdmin.Common.Extensions
{
    public class UniqueConcurrentQueue<T> : ICollection<T>, IEnumerable<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly ConcurrentDictionary<T, byte> _set = new ConcurrentDictionary<T, byte>();

        public bool Enqueue(T item)
        {
            if (_set.TryAdd(item, 0))
            {
                _queue.Enqueue(item);
                return true;
            }
            return false; // 元素已存在，未入队
        }

        public bool TryDequeue(out T item)
        {
            if (_queue.TryDequeue(out item))
            {
                _set.TryRemove(item, out _);
                return true;
            }
            return false;
        }

        public T[] PeekAll()
        {
            return _queue.ToArray();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // 供MongoDB反序列化调用
        public void Add(T item)
        {
            Enqueue(item);
        }

        public int Count => _queue.Count;
        public bool IsReadOnly => false;

        public void Clear()
        {
            while (_queue.TryDequeue(out _)) ;
            _set.Clear();
        }

        public bool Contains(T item) => _set.ContainsKey(item);

        public void CopyTo(T[] array, int arrayIndex)
        {
            _queue.ToArray().CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException("Remove is not supported for UniqueConcurrentQueue.");
        }
    }
}
