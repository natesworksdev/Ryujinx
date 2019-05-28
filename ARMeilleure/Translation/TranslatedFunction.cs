using ARMeilleure.State;

namespace ARMeilleure.Translation
{
    class TranslatedFunction
    {
        private GuestFunction _func;

        public TranslatedFunction(GuestFunction func)
        {
            _func = func;
        }

        public ulong Execute(ExecutionContext context)
        {
            return _func(context.NativeContextPtr);
        }
    }
}