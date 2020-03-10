using System;
<<<<<<< HEAD
using System.Runtime.InteropServices;
=======
>>>>>>> c40a67d26ba5fb0526a6e53668e8f60a82164c0e
using System.Threading;
using System.Runtime.InteropServices;

namespace ARMeilleure.Translation
{
    class TranslatedFunction
    {
        public IntPtr Pointer => Marshal.GetFunctionPointerForDelegate(_func);

        public int EntryCount;

        private const int MinCallsForRejit = 100;

        private GuestFunction _func;

        private ulong _address;
        private bool  _rejit;
        private int   _callCount;

<<<<<<< HEAD
        public bool HighCq => !_rejit;

        public TranslatedFunction(GuestFunction func, bool rejit)
=======
        public TranslatedFunction(GuestFunction func, ulong address, bool rejit)
>>>>>>> c40a67d26ba5fb0526a6e53668e8f60a82164c0e
        {
            _func = func;
            _rejit = rejit;
            _address = address;
        }

        public ulong Execute(State.ExecutionContext context)
        {
            if (Interlocked.Increment(ref EntryCount) == 0)
            {
                return _address;
            }

            var nextAddress = _func(context.NativeContextPtr);

            Interlocked.Decrement(ref EntryCount);

            return nextAddress;
        }

        public bool ShouldRejit()
        {
            return _rejit && Interlocked.Increment(ref _callCount) == MinCallsForRejit;
        }

        public IntPtr GetPointer()
        {
            return Marshal.GetFunctionPointerForDelegate(_func);
        }
    }
}