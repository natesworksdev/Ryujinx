using System;
using System.Collections.Generic;
using System.Threading;
using Ryujinx.Common.Extensions;

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
            using (_treeLock.Write())
            {
                return _tree.AddOrUpdate(address, address + size, value, updateFactoryCallback);
            }
        }

        public T GetOrAdd(ulong address, ulong size, T value)
        {
            using (_treeLock.Write())
            {
                return _tree.GetOrAdd(address, address + size, value);
            }
        }

        public bool Remove(ulong address)
        {
            using (_treeLock.Write())
            {
                return _tree.Remove(address) != 0;
            }
        }

        public void Clear()
        {
            using (_treeLock.Write())
            {
                _tree.Clear();
            }
        }

        public bool ContainsKey(ulong address)
        {
            using (_treeLock.Read())
            {
                return _tree.ContainsKey(address);
            }
        }

        public bool TryGetValue(ulong address, out T value)
        {
            using (_treeLock.Read())
            {
                return _tree.TryGet(address, out value);
            }
        }

        public int GetOverlaps(ulong address, ulong size, ref ulong[] overlaps)
        {
            using (_treeLock.Read())
            {
                return _tree.Get(address, address + size, ref overlaps);
            }
        }

        public List<T> AsList()
        {
            using (_treeLock.Read())
            {
                return _tree.AsList();
            }
        }
    }
}
