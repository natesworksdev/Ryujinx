using System;
using System.Collections.Generic;
using System.Threading;

namespace ARMeilleure.Translation
{
    internal class TranslatorCache<T>
    {
        private readonly IntervalTree<ulong, T> _tree;
        private readonly ReaderWriterLockSlim _treeLock;

        public int Count => _tree.Count;

        public TranslatorCache()
        {
            _tree = new IntervalTree<ulong, T>();
            _treeLock = new ReaderWriterLockSlim();
        }

        public bool TryAdd(ulong address, ulong size, T value)
        {
            return AddOrUpdate(address, size, value, null);
        }

        public bool AddOrUpdate(ulong address, ulong size, T value, Func<ulong, T, T> updateFactoryCallback)
        {
            _treeLock.EnterWriteLock();
            try
            {
                return _tree.AddOrUpdate(address, address + size, value, updateFactoryCallback);
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }

        public T GetOrAdd(ulong address, ulong size, T value)
        {
            _treeLock.EnterWriteLock();
            try
            {
                return _tree.GetOrAdd(address, address + size, value);
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }

        public bool Remove(ulong address)
        {
            _treeLock.EnterWriteLock();
            try
            {
                return _tree.Remove(address) != 0;
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            _treeLock.EnterWriteLock();
            try
            {
                _tree.Clear();
            }
            finally
            {
                _treeLock.ExitWriteLock();
            }
        }

        public bool ContainsKey(ulong address)
        {
            _treeLock.EnterReadLock();
            try
            {
                return _tree.ContainsKey(address);
            }
            finally
            {
                _treeLock.ExitReadLock();
            }
        }

        public bool TryGetValue(ulong address, out T value)
        {
            _treeLock.EnterReadLock();
            try
            {
                return _tree.TryGet(address, out value);
            }
            finally
            {
                _treeLock.ExitReadLock();
            }
        }

        public int GetOverlaps(ulong address, ulong size, ref ulong[] overlaps)
        {
            _treeLock.EnterReadLock();
            try
            {
                return _tree.Get(address, address + size, ref overlaps);
            }
            finally
            {
                _treeLock.ExitReadLock();
            }
        }

        public List<T> AsList()
        {
            _treeLock.EnterReadLock();
            try
            {
                return _tree.AsList();
            }
            finally
            {
                _treeLock.ExitReadLock();
            }
        }
    }
}
