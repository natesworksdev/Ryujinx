using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Common.Cache
{
    struct HashState
    {
        private const ulong M = 0x880355f21e6d1965UL;
        private ulong _hash;
        private int _start;

        public static uint CalcHash(ReadOnlySpan<byte> data)
        {
            HashState state = new HashState();

            state.Initialize();
            state.Continue(data);
            return state.Finalize(data);
        }

        public void Initialize()
        {
            _hash = 23;
        }

        public void Continue(ReadOnlySpan<byte> data)
        {
            ulong h = _hash;

            ReadOnlySpan<ulong> dataAsUlong = MemoryMarshal.Cast<byte, ulong>(data.Slice(_start));

            for (int i = 0; i < dataAsUlong.Length; i++)
            {
                ulong value = dataAsUlong[i];

                h ^= Mix(value);
                h *= M;
            }

            _hash = h;
            _start = data.Length & ~7;
        }

        public uint Finalize(ReadOnlySpan<byte> data)
        {
            ulong h = _hash;

            int remainder = data.Length & 7;
            if (remainder != 0)
            {
                ulong v = 0;

                for (int i = data.Length - remainder; i < data.Length; i++)
                {
                    v |= (ulong)data[i] << ((i - remainder) * 8);
                }

                h ^= Mix(v);
                h *= M;
            }

            h = Mix(h);
            return (uint)(h - (h >> 32));
        }

        private static ulong Mix(ulong h)
        {
            h ^= h >> 23;
            h *= 0x2127599bf4325c37UL;
            h ^= h >> 47;
            return h;
        }
    }
}
