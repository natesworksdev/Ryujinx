using Ryujinx.Graphics.Device;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Gpu.Engine.Threed
{
    struct StateUpdateCallbackEntry
    {
        public Action Callback { get; }
        public string[] FieldNames { get; }

        public StateUpdateCallbackEntry(Action callback, params string[] fieldNames)
        {
            Callback = callback;
            FieldNames = fieldNames;
        }
    }

    class StateUpdateTracker<TState>
    {
        private const int RegisterSize = sizeof(uint);

        private readonly byte[] _registerToGroupMapping;
        private readonly Action[] _callbacks;
        private ulong _dirtyMask;

        public StateUpdateTracker(StateUpdateCallbackEntry[] entries)
        {
            _registerToGroupMapping = new byte[0xe00];
            _callbacks = new Action[entries.Length];

            var fieldToDelegate = new Dictionary<string, int>();

            for (int entryIndex = 0; entryIndex < entries.Length; entryIndex++)
            {
                var entry = entries[entryIndex];

                foreach (var fieldName in entry.FieldNames)
                {
                    fieldToDelegate.Add(fieldName, entryIndex);
                }

                _callbacks[entryIndex] = entry.Callback;
            }

            var fields = typeof(TState).GetFields();
            int offset = 0;

            for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
            {
                var field = fields[fieldIndex];

                int sizeOfField = SizeCalculator.SizeOf(field.FieldType);

                if (fieldToDelegate.TryGetValue(field.Name, out int entryIndex))
                {
                    for (int i = 0; i < ((sizeOfField + 3) & ~3); i += 4)
                    {
                        _registerToGroupMapping[(offset + i) / RegisterSize] = (byte)(entryIndex + 1);
                    }
                }

                offset += sizeOfField;
            }

            Debug.Assert(offset == Unsafe.SizeOf<TState>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDirty(int offset)
        {
            int groupIndex = _registerToGroupMapping[offset / RegisterSize];
            if (groupIndex != 0)
            {
                groupIndex--;
                _dirtyMask |= 1UL << groupIndex;
            }
        }

        public void ForceDirty(int groupIndex)
        {
            if ((uint)groupIndex >= _callbacks.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(groupIndex));
            }

            _dirtyMask |= 1UL << groupIndex;
        }

        public void SetAllDirty()
        {
            Debug.Assert(_callbacks.Length <= sizeof(ulong) * 8);
            _dirtyMask = ulong.MaxValue >> ((sizeof(ulong) * 8) - _callbacks.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ulong checkMask)
        {
            ulong mask = _dirtyMask & checkMask;
            if (mask == 0)
            {
                return;
            }

            do
            {
                int groupIndex = BitOperations.TrailingZeroCount(mask);

                _callbacks[groupIndex]();

                mask &= ~(1UL << groupIndex);
            }
            while (mask != 0);

            _dirtyMask &= ~checkMask;
        }
    }
}
