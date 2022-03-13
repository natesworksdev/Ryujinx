using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ryujinx.Common.Cache
{
    public class PartitionedHashTable<T>
    {
        private struct SizeEntry
        {
            public int Size { get; }
            public int TableCount => _table.Count;

            private readonly PartitionHashTable<T> _table;

            public SizeEntry(int size)
            {
                Size = size;
                _table = new PartitionHashTable<T>();
            }

            public T GetOrAdd(byte[] data, uint dataHash, T item)
            {
                Debug.Assert(data.Length == Size);
                return _table.GetOrAdd(data, dataHash, item);
            }

            public bool Add(byte[] data, uint dataHash, T item)
            {
                Debug.Assert(data.Length == Size);
                return _table.Add(data, dataHash, item);
            }

            public bool AddPartial(byte[] ownerData, uint dataHash)
            {
                return _table.AddPartial(ownerData, dataHash, Size);
            }

            public void FillPartials(SizeEntry newEntry)
            {
                Debug.Assert(newEntry.Size < Size);
                _table.FillPartials(newEntry._table, newEntry.Size);
            }

            public PartitionHashTable<T>.SearchResult TryFindItem(ref SmartDataAccessor dataAccessor, ref T item, ref byte[] data)
            {
                return _table.TryFindItem(ref dataAccessor, Size, ref item, ref data);
            }
        }

        private readonly List<SizeEntry> _sizeTable;

        public PartitionedHashTable()
        {
            _sizeTable = new List<SizeEntry>();
        }

        public void Add(byte[] data, T item)
        {
            GetOrAdd(data, item);
        }

        public T GetOrAdd(byte[] data, T item)
        {
            SizeEntry sizeEntry;

            int index = BinarySearch(_sizeTable, data.Length);
            if (index < _sizeTable.Count && _sizeTable[index].Size == data.Length)
            {
                sizeEntry = _sizeTable[index];
            }
            else
            {
                if (index < _sizeTable.Count && _sizeTable[index].Size < data.Length)
                {
                    index++;
                }

                sizeEntry = new SizeEntry(data.Length);

                _sizeTable.Insert(index, sizeEntry);

                for (int i = index + 1; i < _sizeTable.Count; i++)
                {
                    _sizeTable[i].FillPartials(sizeEntry);
                }
            }

            HashState hashState = new HashState();
            hashState.Initialize();

            for (int i = 0; i < index; i++)
            {
                ReadOnlySpan<byte> dataSlice = new ReadOnlySpan<byte>(data).Slice(0, _sizeTable[i].Size);
                hashState.Continue(dataSlice);
                _sizeTable[i].AddPartial(data, hashState.Finalize(dataSlice));
            }

            hashState.Continue(data);
            return sizeEntry.GetOrAdd(data, hashState.Finalize(data), item);
        }

        private static int BinarySearch(List<SizeEntry> entries, int size)
        {
            int left = 0;
            int middle = 0;
            int right = entries.Count - 1;

            while (left <= right)
            {
                middle = left + ((right - left) >> 1);

                SizeEntry entry = entries[middle];

                if (size == entry.Size)
                {
                    break;
                }

                if (size < entry.Size)
                {
                    right = middle - 1;
                }
                else
                {
                    left = middle + 1;
                }
            }

            return middle;
        }

        public bool TryFindItem(IDataAccessor dataAccessor, out T item, out byte[] data)
        {
            SmartDataAccessor sda = new SmartDataAccessor(dataAccessor);

            item = default;
            data = null;

            int left = 0;
            int right = _sizeTable.Count;

            while (left != right)
            {
                int index = left + ((right - left) >> 1);

                PartitionHashTable<T>.SearchResult result = _sizeTable[index].TryFindItem(ref sda, ref item, ref data);

                if (result == PartitionHashTable<T>.SearchResult.FoundFull)
                {
                    return true;
                }

                if (result == PartitionHashTable<T>.SearchResult.NotFound)
                {
                    right = index;
                }
                else /* if (result == PartitionHashTable<T>.SearchResult.FoundPartial) */
                {
                    left = index + 1;
                }
            }

            data = null;
            return false;
        }

        private int GetSearchStartIndex()
        {
            int idealSize = _sizeTable[0].TableCount;
            int idealIndex = 0;

            for (int i = 1; i < _sizeTable.Count; i++)
            {
                if (_sizeTable[i].TableCount > idealSize)
                {
                    idealSize = _sizeTable[i].TableCount;
                    idealIndex = i;

                }
            }

            return idealIndex;
        }
    }
}
