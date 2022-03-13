using System.Collections.Generic;

namespace Ryujinx.Common.Cache.Serialization
{
    public class SerializableContainer
    {
        private readonly List<byte[]> _entries;

        public SerializableContainer()
        {
            _entries = new List<byte[]>();
        }

        public SerializableDataIndex Add(byte[] data)
        {
            int index = _entries.Count;
            _entries.Add(data);
            return new SerializableDataIndex(index);
        }

        public byte[] Get(SerializableDataIndex dataIndex)
        {
            return _entries[dataIndex.Index];
        }
    }
}