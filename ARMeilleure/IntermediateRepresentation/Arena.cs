using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ARMeilleure.IntermediateRepresentation
{
    unsafe class Arena
    {
        [ThreadStatic]
        private static List<Arena> _instances;

        private static List<Arena> Instances
        {
            get
            {
                if (_instances == null)
                {
                    _instances = new(capacity: 4);
                }

                return _instances;
            }
        }

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

            Instances.Add(this);
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
            foreach (var instance in Instances)
            {
                instance.Reset();
            }
        }
    }

    unsafe class Arena<T> : Arena where T : unmanaged
    {
        [ThreadStatic]
        private static Arena<T> _instance;

        private static Arena<T> Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new(16 * 1024 * 1024);
                }
                
                return _instance;
            }
        }

        public Arena(int capacity) : base(capacity / sizeof(T) * sizeof(T)) { }

        public static T* Alloc(int count = 1)
        {
            return (T*)Instance.Allocate(count * sizeof(T));
        }
    }
}
