using System.Runtime.CompilerServices;

namespace ARMeilleure.Common
{
    internal static class MethodOptions
    {
        internal const MethodImplOptions FastInline = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
    }
}
