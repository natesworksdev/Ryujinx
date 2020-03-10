using System.Threading;

namespace ARMeilleure.Translation
{
    class TranslatedFunction
    {
        private GuestFunction _func;

        private int _callCount;

        public TranslatedFunction(GuestFunction func)
        {
            _func = func;
        }

        public ulong Execute(State.ExecutionContext context)
        {
            return _func(context.NativeContextPtr);
        }

        public int GetCallCount()
        {
            return Interlocked.Increment(ref _callCount);
        }
    }
}