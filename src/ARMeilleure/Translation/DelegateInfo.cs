using System;

namespace ARMeilleure.Translation
{
    class DelegateInfo
    {
        public IntPtr FuncPtr { get; }

        public DelegateInfo(IntPtr funcPtr)
        {
            FuncPtr = funcPtr;
        }
    }
}
