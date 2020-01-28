namespace ARMeilleure.Translation
{
    using PTC;

    class TranslatedFunction
    {
        private readonly int _minCallsForRejit = 75;

        private GuestFunction _func;

        private bool _rejit;
        private int  _callCount;

        public TranslatedFunction(GuestFunction func, bool rejit)
        {
            _func  = func;
            _rejit = rejit;

            if (Ptc.Enabled)
            {
                _minCallsForRejit = 1;
            }
        }

        public ulong Execute(State.ExecutionContext context)
        {
            return _func(context.NativeContextPtr);
        }

        public bool ShouldRejit()
        {
            return _rejit && ++_callCount >= _minCallsForRejit;
        }

        public void ResetRejit()
        {
            _rejit = false;
        }
    }
}