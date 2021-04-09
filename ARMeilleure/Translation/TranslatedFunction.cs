using ARMeilleure.Common;
using System;
using System.Runtime.InteropServices;

namespace ARMeilleure.Translation
{
    class TranslatedFunction
    {
        private readonly GuestFunction _func; // Ensure that this delegate will not be garbage collected.

        public Counter CallCounter { get; }
        public ulong GuestSize { get; }
        public bool HighCq { get; }
        public IntPtr FuncPtr { get; }

        public TranslatedFunction(GuestFunction func, Counter callCounter, ulong guestSize, bool highCq)
        {
            _func = func;
            CallCounter = callCounter;
            GuestSize = guestSize;
            HighCq = highCq;
            FuncPtr = Marshal.GetFunctionPointerForDelegate(func);
        }

        public ulong Execute(State.ExecutionContext context)
        {
            return _func(context.NativeContextPtr);
        }
    }
}