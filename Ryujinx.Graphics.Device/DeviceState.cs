using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Device
{
    public class DeviceState<TState> : IDeviceState where TState : unmanaged
    {
        private const int RegisterSize = sizeof(int);

        public TState State;

        private readonly uint _size;

        private readonly Func<int>[] _readCallbacks;
        private readonly Action<int>[] _writeCallbacks;

        private readonly Dictionary<uint, string> _fieldNamesForDebug;
        private readonly Action<string> _debugLogCallback;

        public DeviceState(IReadOnlyDictionary<string, RwCallback> callbacks = null, Action<string> debugLogCallback = null)
        {
            int size = (Unsafe.SizeOf<TState>() + RegisterSize - 1) / RegisterSize;

            _size = (uint)size;

            _readCallbacks = new Func<int>[size];
            _writeCallbacks = new Action<int>[size];

            if (debugLogCallback != null)
            {
                _fieldNamesForDebug = new Dictionary<uint, string>();
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
                    _fieldNamesForDebug.Add((uint)offset, field.Name);
                }

                offset += sizeOfField;
            }

            Debug.Assert(offset == Unsafe.SizeOf<TState>());
        }

        public int Read(int offset)
        {
            uint index = (uint)offset / RegisterSize;

            if (index < _size)
            {
                uint alignedOffset = index * RegisterSize;

                var readCallback = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_readCallbacks), (IntPtr)index);
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
            uint index = (uint)offset / RegisterSize;

            if (index < _size)
            {
                uint alignedOffset = index * RegisterSize;
                DebugWrite(alignedOffset, data);

                GetRefUnchecked<int>(alignedOffset) = data;

                Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_writeCallbacks), (IntPtr)index)?.Invoke(data);
            }
        }

        public bool WriteWithRedundancyCheck(int offset, int data)
        {
            bool changed = false;
            uint index = (uint)offset / RegisterSize;

            if (index < _size)
            {
                uint alignedOffset = index * RegisterSize;
                DebugWrite(alignedOffset, data);

                ref var storage = ref GetRefUnchecked<int>(alignedOffset);
                changed = storage != data;
                storage = data;

                Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_writeCallbacks), (IntPtr)index)?.Invoke(data);
            }

            return changed;
        }

        [Conditional("DEBUG")]
        private void DebugWrite(uint alignedOffset, int data)
        {
            if (_fieldNamesForDebug != null && _fieldNamesForDebug.TryGetValue(alignedOffset, out string fieldName))
            {
                _debugLogCallback($"{typeof(TState).Name}.{fieldName} = 0x{data:X}");
            }
        }

        public ref T GetRef<T>(int offset) where T : unmanaged
        {
            if ((uint)(offset + Unsafe.SizeOf<T>()) > Unsafe.SizeOf<TState>())
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            return ref GetRefUnchecked<T>((uint)offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref T GetRefUnchecked<T>(uint offset) where T : unmanaged
        {
            return ref Unsafe.As<TState, T>(ref Unsafe.AddByteOffset(ref State, (IntPtr)offset));
        }
    }
}
