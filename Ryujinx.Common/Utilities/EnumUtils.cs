using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ryujinx.Common
{
    public static class EnumUtils
    {
        [MethodImpl(MethodOptions.FastInline)]
        [return: NotNull]
        public static T[] GetValues<T>() where T : Enum => (T[])Enum.GetValues(typeof(T));

        [MethodImpl(MethodOptions.FastInline)]
        public static T GetMaxValue<T>() where T : Enum => GetValues<T>().Max();

        [MethodImpl(MethodOptions.FastInline)]
        public static int GetCount<T>() where T : Enum => Enum.GetValues(typeof(T)).Length;
    }
}
