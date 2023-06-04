using ARMeilleure.IntermediateRepresentation;

namespace ARMeilleure.CodeGen.RegisterAllocators
{
    class StackAllocator
    {
        public int TotalSize { get; private set; }

        public int Allocate(OperandType type)
        {
            return Allocate(type.GetSizeInBytes());
        }

        public int Allocate(int sizeInBytes)
        {
            int offset = TotalSize;

            TotalSize += sizeInBytes;

            return offset;
        }
    }
}