using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ARMeilleure.IntermediateRepresentation
{
    unsafe struct MemoryOperand
    {
        private struct Data
        {
            public byte Kind;
            public byte Type;
            public byte Scale;
            public Operand BaseAddress;
            public Operand Index;
            public int Displacement;
        }

        private Data* _data;

        public Operand BaseAddress
        {
            get => _data->BaseAddress;
            set => _data->BaseAddress = value; 
        }

        public Operand Index
        {
            get => _data->Index;
            set => _data->Index = value; 
        }

        public Multiplier Scale
        {
            get => (Multiplier)_data->Scale;
            set => _data->Scale = (byte)value;
        }

        public int Displacement
        {
            get => _data->Displacement;
            set => _data->Displacement = value;
        }

        public static MemoryOperand Cast(Operand operand)
        {
            Debug.Assert(operand.Kind == OperandKind.Memory);

            return Unsafe.As<Operand, MemoryOperand>(ref operand);
        }
    }
}