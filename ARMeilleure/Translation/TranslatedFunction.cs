using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ARMeilleure.Translation
{
    class TranslatedFunction
    {
        private GuestFunction _func;

        private bool _rejit;
        private int _callCount;

        public bool HighCq => !_rejit; // TODO: .

        public TranslatedFunction(GuestFunction func, bool rejit)
        {
            _func = func;
            _rejit = rejit;
        }

        public ulong Execute(State.ExecutionContext context)
        {
            return _func(context.NativeContextPtr);
        }

        public int GetCallCount()
        {
            return Interlocked.Increment(ref _callCount);
        }

        public IntPtr GetPointer()
        {
            return Marshal.GetFunctionPointerForDelegate(_func);
        }
    }
}