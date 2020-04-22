using System;
using System.Runtime.CompilerServices;

namespace ARMeilleure.Common
{
    class EnumUtils
    {
        [MethodImpl(MethodOptions.FastInline)]
        internal static int GetCount<T>() where T : Enum => Enum.GetValues(typeof(T)).Length;
    }
}
