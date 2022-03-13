using System;
using System.Collections.Generic;

namespace Ryujinx.Common.Cache
{
    ref struct SmartDataAccessor
    {
        private readonly IDataAccessor _dataAccessor;
        private ReadOnlySpan<byte> _data;
        private readonly SortedList<int, HashState> _cachedHashes;

        public SmartDataAccessor(IDataAccessor dataAccessor)
        {
            _dataAccessor = dataAccessor;
            _data = ReadOnlySpan<byte>.Empty;
            _cachedHashes = new SortedList<int, HashState>();
        }

        public ReadOnlySpan<byte> GetSpan(int size)
        {
            if (_data.Length < size)
            {
                _data = _dataAccessor.GetSpan(0, size);
            }
            else if (_data.Length > size)
            {
                return _data.Slice(0, size);
            }

            return _data;
        }

        public ReadOnlySpan<byte> GetSpanAndHash(int size, out uint hash)
        {
            ReadOnlySpan<byte> data = GetSpan(size);
            hash = data.Length == size ? CalcHashCached(data) : 0;
            return data;
        }

        private uint CalcHashCached(ReadOnlySpan<byte> data)
        {
            HashState state = default;
            bool found = false;

            for (int i = _cachedHashes.Count - 1; i >= 0; i--)
            {
                int cachedHashSize = _cachedHashes.Keys[i];

                if (cachedHashSize < data.Length)
                {
                    state = _cachedHashes.Values[i];
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                state = new HashState();
                state.Initialize();
            }

            state.Continue(data);
            _cachedHashes[data.Length & ~7] = state;
            return state.Finalize(data);
        }
    }
}
