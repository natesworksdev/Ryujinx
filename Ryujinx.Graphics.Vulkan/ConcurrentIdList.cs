using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.Graphics.Vulkan
{
    class ConcurrentIdList<T> where T : class
    {
        private readonly List<T> _list;
        private readonly Queue<int> _freeHint;
        private readonly ReaderWriterLock _lock;

        public ConcurrentIdList()
        {
            _list = new List<T>();
            _freeHint = new Queue<int>();
            _lock = new ReaderWriterLock();
        }

        public int Add(T value)
        {
            int id;

            _lock.AcquireWriterLock(Timeout.Infinite);

            try
            {
                if (_freeHint.TryDequeue(out id))
                {
                    _list[id] = value;
                }
                else
                {
                    id = _list.Count;

                    _list.Add(value);
                }
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }

            return id + 1;
        }

        public void Remove(int id)
        {
            id--;

            _lock.AcquireWriterLock(Timeout.Infinite);

            try
            {
                int count = _list.Count;

                if ((uint)id >= (uint)count)
                {
                    return;
                }

                if (id + 1 == count)
                {
                    int removeIndex = id;

                    while (removeIndex > 0 && _list[removeIndex - 1] == null)
                    {
                        removeIndex--;
                    }

                    _list.RemoveRange(removeIndex, count - removeIndex);
                }
                else
                {
                    _list[id] = null;
                    _freeHint.Enqueue(id);
                }
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
        }

        public bool TryGetValue(int id, out T value)
        {
            id--;

            _lock.AcquireReaderLock(Timeout.Infinite);

            try
            {
                int count = _list.Count;

                if ((uint)id >= (uint)count || _list[id] == null)
                {
                    value = null;
                    return false;
                }

                value = _list[id];
                return true;
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }
        }

        public void Clear()
        {
            _lock.AcquireWriterLock(Timeout.Infinite);

            try
            {
                _list.Clear();
                _freeHint.Clear();
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            _lock.AcquireWriterLock(Timeout.Infinite);

            try
            {
                for (int i = 0; i < _list.Count; i++)
                {
                    if (_list[i] != null)
                    {
                        yield return _list[i];
                    }
                }
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
        }
    }
}