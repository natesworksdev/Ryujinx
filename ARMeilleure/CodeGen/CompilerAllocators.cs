using ARMeilleure.Common;
using System.Diagnostics;

namespace ARMeilleure.CodeGen
{
    class CompilerAllocators
    {
        private int _referenceCount;

        public ArenaAllocator Default { get; } = new(256 * 1024, 4);
        public ArenaAllocator Operands { get; } = new(64 * 1024, 8);
        public ArenaAllocator Operations { get; } = new(64 * 1024, 8);
        public ArenaAllocator References { get; } = new(64 * 1024, 8);

        internal void IncrementReferenceCount()
        {
            _referenceCount++;
        }

        internal void DecrementReferenceCount()
        {
            _referenceCount--;

            // No more references to the allocators, we can safely reset them.
            if (_referenceCount == 0)
            {
                Default.Reset();
                Operands.Reset();
                Operations.Reset();
                References.Reset();
            }

            Debug.Assert(_referenceCount >= 0);
        }
    }
}
