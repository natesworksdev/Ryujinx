using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk
{
    ref struct Writer
    {
        private Span<byte> _output;

        public Writer(Span<byte> output)
        {
            _output = output;
        }

        public void Write<T>(T value) where T : unmanaged
        {
            MemoryMarshal.Cast<byte, T>(_output)[0] = value;
            _output = _output.Slice(Unsafe.SizeOf<T>());
        }
    }
}
