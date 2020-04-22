using System.Runtime.CompilerServices;

namespace ARMeilleure.Common
{
    static class MethodOptions
    {
        public const MethodImplOptions FastInline = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
    }
}
