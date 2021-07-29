using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ARMeilleure.IntermediateRepresentation
{
    unsafe class Arena
    {
        [ThreadStatic]
        private static readonly List<Arena> _instances = new(capacity: 4);

        private int _index;
        private readonly int _capacity;
        private readonly byte* _block;

        public Arena(int capacity)
        {
            _index = 0;
            _capacity = capacity;
            _block = (byte*)Marshal.AllocHGlobal(_capacity);

            if (_block == null)
            {
                throw new OutOfMemoryException();
            }

            _instances.Add(this);
        }

        public byte* Allocate(int bytes)
        {
            if (_index + bytes > _capacity)
            {
                throw new OutOfMemoryException();
            }

            byte* result = &_block[_index];

            _index += bytes;

            return result;
        }

        public void Reset()
        {
            _index = 0;
        }

        public static void ResetAll()
        {
            foreach (var instance in _instances)
            {
                instance.Reset();
            }
        }
    }

    unsafe class Arena<T> : Arena where T : unmanaged
    {
        [ThreadStatic]
        private static readonly Arena<T> _instance = new(4 * 1024 * 1024);

        public Arena(int capacity) : base(capacity / sizeof(T) * sizeof(T)) { }

        public static T* Alloc(int count = 1)
        {
            return (T*)_instance.Allocate(count * sizeof(T));
        }
    }
}
