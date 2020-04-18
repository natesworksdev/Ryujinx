using System.Runtime.CompilerServices;

namespace Ryujinx.Common
{
    public static class MethodOptions
    {
        internal const MethodImplOptions FastInline = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
    }
}
