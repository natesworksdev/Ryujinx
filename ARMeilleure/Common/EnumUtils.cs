using System;
using System.Runtime.CompilerServices;

namespace ARMeilleure.Common
{
    static class EnumUtils
    {
        [MethodImpl(MethodOptions.FastInline)]
        public static int GetCount<T>() where T : Enum => Enum.GetValues(typeof(T)).Length;
    }
}
