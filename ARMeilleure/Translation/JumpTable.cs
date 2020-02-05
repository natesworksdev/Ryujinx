using ARMeilleure.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace ARMeilleure.Translation
{
    class JumpTable
    {
        // The jump table is a block of (guestAddress, hostAddress) function mappings.
        // Each entry corresponds to one branch in a JIT compiled function. The entries are
        // reserved specifically for each call.
        // The _dependants dictionary can be used to update the hostAddress for any functions that change.

        public const int JumpTableStride = 16; // 8 byte guest address, 8 byte host address

        private const int JumpTableSize = 1048576;

        private const int JumpTableByteSize = JumpTableSize * JumpTableStride;

        // The dynamic table is also a block of (guestAddress, hostAddress) function mappings.
        // The main difference is that indirect calls and jumps reserve _multiple_ entries on the table.
        // These start out as all 0. When an indirect call is made, it tries to find the guest address on the table.

        // If we get to an empty address, the guestAddress is set to the call that we want.

        // If we get to a guestAddress that matches our own (or we just claimed it), the hostAddress is read.
        // If it is non-zero, we immediately branch or call the host function.
        // If it is 0, NativeInterface is called to find the rejited address of the call.
        // If none is found, the hostAddress entry stays at 0. Otherwise, the new address is placed in the entry.

        // If the table size is exhausted and we didn't find our desired address, we fall back to doing requesting 
        // the function from the JIT.

        private const int DynamicTableSize = 1048576;

        public const int DynamicTableElems = 10;

        private const int DynamicTableByteSize = DynamicTableSize * JumpTableStride * DynamicTableElems;

        private int _tableEnd = 0;
        private int _dynTableEnd = 0;

        private ConcurrentDictionary<ulong, TranslatedFunction> _targets;
        private ConcurrentDictionary<ulong, LinkedList<int>> _dependants; // TODO: Attach to TranslatedFunction or a wrapper class.

        public IntPtr BasePointer { get; }
        public IntPtr DynamicPointer { get; }

        public JumpTable()
        {
            BasePointer = MemoryManagement.Allocate(JumpTableByteSize);
            DynamicPointer = MemoryManagement.Allocate(DynamicTableByteSize);

            _targets = new ConcurrentDictionary<ulong, TranslatedFunction>();
            _dependants = new ConcurrentDictionary<ulong, LinkedList<int>>();
        }

        public void RegisterFunction(ulong address, TranslatedFunction func) {
            address &= ~3UL;
            _targets.AddOrUpdate(address, func, (key, oldFunc) => func);
            long funcPtr = func.GetPointer().ToInt64();

            // Update all jump table entries that target this address.
            LinkedList<int> myDependants;
            if (_dependants.TryGetValue(address, out myDependants)) {
                lock (myDependants)
                {
                    foreach (var entry in myDependants)
                    {
                        IntPtr addr = BasePointer + entry * JumpTableStride;
                        Marshal.WriteInt64(addr, 8, funcPtr);
                    }
                }
            }
        }

        public ulong TryGetFunction(ulong address)
        {
            TranslatedFunction result;
            if (_targets.TryGetValue(address, out result))
            {
                return (ulong)result.GetPointer().ToInt64();
            }
            return 0;
        }

        public int ReserveDynamicEntry()
        {
            int entry = Interlocked.Increment(ref _dynTableEnd);
            if (entry >= DynamicTableSize)
            {
                throw new OutOfMemoryException("JIT Dynamic Jump Table Exhausted.");
            }
            return entry;
        }

        public int ReserveTableEntry(long ownerAddress, long address)
        {
            int entry = Interlocked.Increment(ref _tableEnd);
            if (entry >= JumpTableSize)
            {
                throw new OutOfMemoryException("JIT Direct Jump Table Exhausted.");
            }

            // Is the address we have already registered? If so, put the function address in the jump table.
            long value = 0;
            TranslatedFunction func;
            if (_targets.TryGetValue((ulong)address, out func))
            {
                value = func.GetPointer().ToInt64();
            }

            // Make sure changes to the function at the target address update this jump table entry.
            LinkedList<int> targetDependants = _dependants.GetOrAdd((ulong)address, (addr) => new LinkedList<int>());
            targetDependants.AddLast(entry);

            IntPtr addr = BasePointer + entry * JumpTableStride;

            Marshal.WriteInt64(addr, 0, address);
            Marshal.WriteInt64(addr, 8, value);

            return entry;
        }
    }
}
