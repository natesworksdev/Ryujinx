namespace ARMeilleure.Translation
{
    class TranslatedFunction
    {
        private GuestFunction _func;

        private bool _rejit;

        public TranslatedFunction(GuestFunction func, bool rejit)
        {
            _func  = func;
            _rejit = rejit;
        }

        public ulong Execute(State.ExecutionContext context)
        {
            return _func(context.NativeContextPtr);
        }

        public bool GetRejit()
        {
            return _rejit;
        }

        public void ResetRejit()
        {
            _rejit = false;
        }
    }
}