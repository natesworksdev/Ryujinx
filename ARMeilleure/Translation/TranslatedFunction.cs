namespace ARMeilleure.Translation
{
    class TranslatedFunction
    {
        private GuestFunction _func;

        public bool Rejit { get; private set; }

        public TranslatedFunction(GuestFunction func, bool rejit)
        {
            _func = func;
            Rejit = rejit;
        }

        public ulong Execute(State.ExecutionContext context)
        {
            return _func(context.NativeContextPtr);
        }

        public void ResetRejit()
        {
            Rejit = false;
        }
    }
}