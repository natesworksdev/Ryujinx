using ARMeilleure.Common;
using System;

namespace ARMeilleure
{
    static class Allocators
    {
        [ThreadStatic] private static ArenaAllocator _default;
        [ThreadStatic] private static ArenaAllocator _operands;
        [ThreadStatic] private static ArenaAllocator _operations;

        public static ArenaAllocator Default => GetAllocator(ref _default);
        public static ArenaAllocator Operands => GetAllocator(ref _operands);
        public static ArenaAllocator Operations => GetAllocator(ref _operations);

        private static ArenaAllocator GetAllocator(ref ArenaAllocator alloc)
        {
            if (alloc == null)
            {
                alloc = new ArenaAllocator();
            }

            return alloc;
        }

        public static void ResetAll()
        {
            Default.Reset();
            Operands.Reset();
            Operations.Reset();
        }
    }
}
