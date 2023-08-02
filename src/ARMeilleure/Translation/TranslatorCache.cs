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
            try
            {
                _treeLock.TryEnterWriteLock(Timeout.Infinite);

                return _tree.AddOrUpdate(address, address + size, value, updateFactoryCallback);
            }
            finally
            {
                if (_treeLock.IsWriteLockHeld)
                {
                    _treeLock.ExitWriteLock();
                }
            }
        }

        public T GetOrAdd(ulong address, ulong size, T value)
        {
            try
            {
                _treeLock.TryEnterWriteLock(Timeout.Infinite);

                return _tree.GetOrAdd(address, address + size, value);
            }
            finally
            {
                if (_treeLock.IsWriteLockHeld)
                {
                    _treeLock.ExitWriteLock();
                }
            }
        }

        public bool Remove(ulong address)
        {
            try
            {
                _treeLock.TryEnterWriteLock(Timeout.Infinite);

                return _tree.Remove(address) != 0;
            }
            finally
            {
                if (_treeLock.IsWriteLockHeld)
                {
                    _treeLock.ExitWriteLock();
                }
            }
        }

        public void Clear()
        {
            try
            {
                _treeLock.TryEnterWriteLock(Timeout.Infinite);
                _tree.Clear();
            }
            finally
            {
                if (_treeLock.IsWriteLockHeld)
                {
                    _treeLock.ExitWriteLock();
                }
            }
        }

        public bool ContainsKey(ulong address)
        {
            try
            {
                _treeLock.TryEnterReadLock(Timeout.Infinite);

                return _tree.ContainsKey(address);
            }
            finally
            {
                if (_treeLock.IsReadLockHeld)
                {
                    _treeLock.ExitReadLock();
                }
            }
        }

        public bool TryGetValue(ulong address, out T value)
        {
            try
            {
                _treeLock.TryEnterReadLock(Timeout.Infinite);

                return _tree.TryGet(address, out value);
            }
            finally
            {
                if (_treeLock.IsReadLockHeld)
                {
                    _treeLock.ExitReadLock();
                }
            }
        }

        public int GetOverlaps(ulong address, ulong size, ref ulong[] overlaps)
        {
            try
            {
                _treeLock.TryEnterReadLock(Timeout.Infinite);

                return _tree.Get(address, address + size, ref overlaps);
            }
            finally
            {
                if (_treeLock.IsReadLockHeld)
                {
                    _treeLock.ExitReadLock();
                }
            }
        }

        public List<T> AsList()
        {
            try
            {
                _treeLock.TryEnterReadLock(Timeout.Infinite);

                return _tree.AsList();
            }
            finally
            {
                if (_treeLock.IsReadLockHeld)
                {
                    _treeLock.ExitReadLock();
                }
            }
        }
    }
}
