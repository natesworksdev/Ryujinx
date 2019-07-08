using ARMeilleure.Memory;
using ARMeilleure.State;
using System.Collections.Concurrent;

using Thread = System.Threading.Thread;

namespace ARMeilleure.Instructions
{
    static class NativeInterface
    {
        private const int ErgSizeLog2 = 4;

        private class ThreadContext
        {
            public ExecutionContext Context { get; }
            public MemoryManager    Memory  { get; }

            public ulong ExclusiveAddress   { get; set; }
            public ulong ExclusiveValueLow  { get; set; }
            public ulong ExclusiveValueHigh { get; set; }

            public ThreadContext(ExecutionContext context, MemoryManager memory)
            {
                Context = context;
                Memory  = memory;

                ExclusiveAddress = ulong.MaxValue;
            }
        }

        private static ConcurrentDictionary<Thread, ThreadContext> _contexts;

        static NativeInterface()
        {
            _contexts = new ConcurrentDictionary<Thread, ThreadContext>();
        }

        public static void RegisterThread(ExecutionContext context, MemoryManager memory)
        {
            _contexts.TryAdd(Thread.CurrentThread, new ThreadContext(context, memory));
        }

        public static void UnregisterThread()
        {
            _contexts.TryRemove(Thread.CurrentThread, out _);
        }

        public static void Break(ulong address, int imm)
        {
            GetContext().OnBreak(address, imm);
        }

        public static void SupervisorCall(ulong address, int imm)
        {
            GetContext().OnSupervisorCall(address, imm);
        }

        public static void Undefined(ulong address, int opCode)
        {
            GetContext().OnUndefined(address, opCode);
        }

#region "System registers"
     public static ulong GetCtrEl0()
     {
         return (ulong)GetContext().CtrEl0;
     }

     public static ulong GetDczidEl0()
     {
         return (ulong)GetContext().DczidEl0;
     }

     public static ulong GetFpcr()
     {
         return (ulong)GetContext().Fpcr;
     }

     public static ulong GetFpsr()
     {
         return (ulong)GetContext().Fpsr;
     }

     public static ulong GetTpidrEl0()
     {
         return (ulong)GetContext().TpidrEl0;
     }

     public static ulong GetTpidr()
     {
         return (ulong)GetContext().Tpidr;
     }

     public static ulong GetCntfrqEl0()
     {
         return GetContext().CntfrqEl0;
     }

     public static ulong GetCntpctEl0()
     {
         return GetContext().CntpctEl0;
     }

     public static void SetFpcr(ulong value)
     {
         GetContext().Fpcr = (FPCR)value;
     }

     public static void SetFpsr(ulong value)
     {
         GetContext().Fpsr = (FPSR)value;
     }

     public static void SetTpidrEl0(ulong value)
     {
         GetContext().TpidrEl0 = (long)value;
     }
#endregion

#region "Read"
        public static byte ReadByte(ulong address)
        {
            return GetMemoryManager().ReadByte((long)address);
        }

        public static ushort ReadUInt16(ulong address)
        {
            return GetMemoryManager().ReadUInt16((long)address);
        }

        public static uint ReadUInt32(ulong address)
        {
            return GetMemoryManager().ReadUInt32((long)address);
        }

        public static ulong ReadUInt64(ulong address)
        {
            return GetMemoryManager().ReadUInt64((long)address);
        }

        public static V128 ReadVector128(ulong address)
        {
            return GetMemoryManager().ReadVector128((long)address);
        }
#endregion

#region "Read exclusive"
        public static byte ReadByteExclusive(ulong address)
        {
            ThreadContext context = GetCurrentContext();

            byte value = context.Memory.ReadByte((long)address);

            context.ExclusiveAddress   = GetMaskedExclusiveAddress(address);
            context.ExclusiveValueLow  = value;
            context.ExclusiveValueHigh = 0;

            return value;
        }

        public static ushort ReadUInt16Exclusive(ulong address)
        {
            ThreadContext context = GetCurrentContext();

            ushort value = context.Memory.ReadUInt16((long)address);

            context.ExclusiveAddress   = GetMaskedExclusiveAddress(address);
            context.ExclusiveValueLow  = value;
            context.ExclusiveValueHigh = 0;

            return value;
        }

        public static uint ReadUInt32Exclusive(ulong address)
        {
            ThreadContext context = GetCurrentContext();

            uint value = context.Memory.ReadUInt32((long)address);

            context.ExclusiveAddress   = GetMaskedExclusiveAddress(address);
            context.ExclusiveValueLow  = value;
            context.ExclusiveValueHigh = 0;

            return value;
        }

        public static ulong ReadUInt64Exclusive(ulong address)
        {
            ThreadContext context = GetCurrentContext();

            ulong value = context.Memory.ReadUInt64((long)address);

            context.ExclusiveAddress   = GetMaskedExclusiveAddress(address);
            context.ExclusiveValueLow  = value;
            context.ExclusiveValueHigh = 0;

            return value;
        }

        public static V128 ReadVector128Exclusive(ulong address)
        {
            ThreadContext context = GetCurrentContext();

            V128 value = context.Memory.AtomicLoadInt128((long)address);

            context.ExclusiveAddress   = GetMaskedExclusiveAddress(address);
            context.ExclusiveValueLow  = value.GetUInt64(0);
            context.ExclusiveValueHigh = value.GetUInt64(1);

            return value;
        }
#endregion

#region "Write"
        public static void WriteByte(ulong address, byte value)
        {
            GetMemoryManager().WriteByte((long)address, value);
        }

        public static void WriteUInt16(ulong address, ushort value)
        {
            GetMemoryManager().WriteUInt16((long)address, value);
        }

        public static void WriteUInt32(ulong address, uint value)
        {
            GetMemoryManager().WriteUInt32((long)address, value);
        }

        public static void WriteUInt64(ulong address, ulong value)
        {
            GetMemoryManager().WriteUInt64((long)address, value);
        }

        public static void WriteVector128(ulong address, V128 value)
        {
            GetMemoryManager().WriteVector128((long)address, value);
        }
#endregion

#region "Write exclusive"
        public static int WriteByteExclusive(ulong address, byte value)
        {
            ThreadContext context = GetCurrentContext();

            bool success = context.ExclusiveAddress == GetMaskedExclusiveAddress(address);

            if (success)
            {
                success = context.Memory.AtomicCompareExchangeByte(
                    (long)address,
                    (byte)context.ExclusiveValueLow,
                    (byte)value);

                if (success)
                {
                    ClearExclusive();
                }
            }

            return success ? 0 : 1;
        }

        public static int WriteUInt16Exclusive(ulong address, ushort value)
        {
            ThreadContext context = GetCurrentContext();

            bool success = context.ExclusiveAddress == GetMaskedExclusiveAddress(address);

            if (success)
            {
                success = context.Memory.AtomicCompareExchangeInt16(
                    (long)address,
                    (short)context.ExclusiveValueLow,
                    (short)value);

                if (success)
                {
                    ClearExclusive();
                }
            }

            return success ? 0 : 1;
        }

        public static int WriteUInt32Exclusive(ulong address, uint value)
        {
            ThreadContext context = GetCurrentContext();

            bool success = context.ExclusiveAddress == GetMaskedExclusiveAddress(address);

            if (success)
            {
                success = context.Memory.AtomicCompareExchangeInt32(
                    (long)address,
                    (int)context.ExclusiveValueLow,
                    (int)value);

                if (success)
                {
                    ClearExclusive();
                }
            }

            return success ? 0 : 1;
        }

        public static int WriteUInt64Exclusive(ulong address, ulong value)
        {
            ThreadContext context = GetCurrentContext();

            bool success = context.ExclusiveAddress == GetMaskedExclusiveAddress(address);

            if (success)
            {
                success = context.Memory.AtomicCompareExchangeInt64(
                    (long)address,
                    (long)context.ExclusiveValueLow,
                    (long)value);

                if (success)
                {
                    ClearExclusive();
                }
            }

            return success ? 0 : 1;
        }

        public static int WriteVector128Exclusive(ulong address, V128 value)
        {
            ThreadContext context = GetCurrentContext();

            bool success = context.ExclusiveAddress == GetMaskedExclusiveAddress(address);

            if (success)
            {
                V128 expected = new V128(context.ExclusiveValueLow, context.ExclusiveValueHigh);

                success = context.Memory.AtomicCompareExchangeInt128((long)address, expected, value);

                if (success)
                {
                    ClearExclusive();
                }
            }

            return success ? 0 : 1;
        }
#endregion

        private static ulong GetMaskedExclusiveAddress(ulong address)
        {
            return address & ~((4UL << ErgSizeLog2) - 1);
        }

        public static void ClearExclusive()
        {
            GetCurrentContext().ExclusiveAddress = ulong.MaxValue;
        }

        public static void CheckSynchronization()
        {
            GetContext().CheckInterrupt();
        }

        private static ThreadContext GetCurrentContext()
        {
            return _contexts[Thread.CurrentThread];
        }

        public static ExecutionContext GetContext()
        {
            return _contexts[Thread.CurrentThread].Context;
        }

        public static MemoryManager GetMemoryManager()
        {
            return _contexts[Thread.CurrentThread].Memory;
        }
    }
}