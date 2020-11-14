using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ARMeilleure.Translation
{
    sealed class TranslatedFunction
    {
        private const int MinCallsForRejit = 100;

        private readonly GuestFunction _func; // Ensure that this delegate will not be garbage collected.

        private int _callCount = 0;

        public ulong  GuestSize { get; }
        public bool   HighCq    { get; }
        public IntPtr FuncPtr   { get; }

        public TranslatedFunction(GuestFunction func, ulong guestSize, bool highCq)
        {
            _func = func;

            GuestSize = guestSize;
            HighCq    = highCq;
            FuncPtr   = Marshal.GetFunctionPointerForDelegate<GuestFunction>(func);
        }

        public ulong Execute(State.ExecutionContext context)
        {
            return _func(context.NativeContextPtr);
        }

        public bool ShouldRejit()
        {
            return !HighCq && Interlocked.Increment(ref _callCount) == MinCallsForRejit;
        }
    }
}