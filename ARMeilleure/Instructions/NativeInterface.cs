using ARMeilleure.State;
using System.Collections.Concurrent;

using Thread = System.Threading.Thread;

namespace ARMeilleure.Instructions
{
    static class NativeInterface
    {
        private static ConcurrentDictionary<Thread, ExecutionContext> _contexts;

        static NativeInterface()
        {
            _contexts = new ConcurrentDictionary<Thread, ExecutionContext>();
        }

        public static void RegisterThread(ExecutionContext context)
        {
            _contexts.TryAdd(Thread.CurrentThread, context);
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

        public static ExecutionContext GetContext()
        {
            return _contexts[Thread.CurrentThread];
        }
    }
}