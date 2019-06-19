using ARMeilleure.Memory;
using ARMeilleure.State;
using System.Collections.Concurrent;

using Thread = System.Threading.Thread;

namespace ARMeilleure.Instructions
{
    static class NativeInterface
    {
        private struct ThreadContext
        {
            public ExecutionContext Context { get; }
            public MemoryManager    Memory  { get; }

            public ThreadContext(ExecutionContext context, MemoryManager memory)
            {
                Context = context;
                Memory  = memory;
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

        public static V128 ReadVector8(ulong address)
        {
            return new V128(0); //TODO
        }

        public static V128 ReadVector16(ulong address)
        {
            return new V128(0); //TODO
        }

        public static V128 ReadVector32(ulong address)
        {
            return new V128(0); //TODO
        }

        public static V128 ReadVector64(ulong address)
        {
            return new V128(0); //TODO
        }

        public static V128 ReadVector128(ulong address)
        {
            return new V128(0); //TODO
        }

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

        public static void WriteVector8(ulong address, V128 value)
        {
            //TODO
        }

        public static void WriteVector16(ulong address, V128 value)
        {
            //TODO
        }

        public static void WriteVector32(ulong address, V128 value)
        {
            //TODO
        }

        public static void WriteVector64(ulong address, V128 value)
        {
            //TODO
        }

        public static void WriteVector128(ulong address, V128 value)
        {
            //TODO
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