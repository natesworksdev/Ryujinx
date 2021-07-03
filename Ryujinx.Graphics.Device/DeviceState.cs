using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Device
{
    public class DeviceState<TState> : IDeviceState where TState : unmanaged
    {
        private const int RegisterSize = sizeof(int);

        public TState State;

        private readonly int _size;

        private readonly Func<int>[] _readCallbacks;
        private readonly Action<int>[] _writeCallbacks;

        private readonly Dictionary<int, string> _fieldNamesForDebug;
        private readonly Action<string> _debugLogCallback;

        public DeviceState(IReadOnlyDictionary<string, RwCallback> callbacks = null, Action<string> debugLogCallback = null)
        {
            int size = (Unsafe.SizeOf<TState>() + RegisterSize - 1) / RegisterSize;

            _size = size;

            _readCallbacks = new Func<int>[size];
            _writeCallbacks = new Action<int>[size];

            if (debugLogCallback != null)
            {
                _fieldNamesForDebug = new Dictionary<int, string>();
                _debugLogCallback = debugLogCallback;
            }

            var fields = typeof(TState).GetFields();
            int offset = 0;

            for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
            {
                var field = fields[fieldIndex];

                int sizeOfField = SizeCalculator.SizeOf(field.FieldType);

                for (int i = 0; i < ((sizeOfField + 3) & ~3); i += 4)
                {
                    int index = (offset + i) / RegisterSize;

                    if (callbacks != null && callbacks.TryGetValue(field.Name, out var cb))
                    {
                        if (cb.Read != null)
                        {
                            _readCallbacks[index] = cb.Read;
                        }

                        if (cb.Write != null)
                        {
                            _writeCallbacks[index] = cb.Write;
                        }
                    }
                }

                if (debugLogCallback != null)
                {
                    _fieldNamesForDebug.Add(offset, field.Name);
                }

                offset += sizeOfField;
            }

            Debug.Assert(offset == Unsafe.SizeOf<TState>());
        }

        public int Read(int offset)
        {
            int index = offset / RegisterSize;

            if ((uint)index < _size)
            {
                int alignedOffset = index * RegisterSize;

                var readCallback = _readCallbacks[index];
                if (readCallback != null)
                {
                    return readCallback();
                }
                else
                {
                    return GetRefUnchecked<int>(alignedOffset);
                }
            }

            return 0;
        }

        public void Write(int offset, int data)
        {
            int index = offset / RegisterSize;

            if ((uint)index < _size)
            {
                int alignedOffset = index * RegisterSize;

#if DEBUG
                if (_fieldNamesForDebug != null && _fieldNamesForDebug.TryGetValue(alignedOffset, out string fieldName))
                {
                    _debugLogCallback($"{typeof(TState).Name}.{fieldName} = 0x{data:X}");
                }
#endif

                GetRefUnchecked<int>(alignedOffset) = data;

                _writeCallbacks[index]?.Invoke(data);
            }
        }

        public ref T GetRef<T>(int offset) where T : unmanaged
        {
            if ((uint)(offset + Unsafe.SizeOf<T>()) > Unsafe.SizeOf<TState>())
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            return ref GetRefUnchecked<T>(offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref T GetRefUnchecked<T>(int offset) where T : unmanaged
        {
            return ref Unsafe.As<TState, T>(ref Unsafe.AddByteOffset(ref State, (IntPtr)offset));
        }
    }
}
