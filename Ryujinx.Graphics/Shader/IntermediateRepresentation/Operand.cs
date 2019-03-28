using Ryujinx.Graphics.Shader.Decoders;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.IntermediateRepresentation
{
    class Operand
    {
        private const int AttrSlotBits = 5;
        private const int AttrSlotLsb  = 32 - AttrSlotBits;
        private const int AttrSlotMask = (1 << AttrSlotBits) - 1;

        public OperandType Type { get; }

        public int Value { get; }

        public INode AsgOp { get; set; }

        public HashSet<INode> UseOps { get; }

        private Operand()
        {
            UseOps = new HashSet<INode>();
        }

        public Operand(OperandType type) : this()
        {
            Type = type;
        }

        public Operand(OperandType type, int value) : this()
        {
            Type  = type;
            Value = value;
        }

        public Operand(Register reg) : this()
        {
            Type  = OperandType.Register;
            Value = PackRegInfo(reg.Index, reg.Type);
        }

        public Operand(int slot, int offset) : this()
        {
            Type  = OperandType.ConstantBuffer;
            Value = PackCbufInfo(slot, offset);
        }

        private static int PackCbufInfo(int slot, int offset)
        {
            return (slot << AttrSlotLsb) | offset;
        }

        private static int PackRegInfo(int index, RegisterType type)
        {
            return ((int)type << 24) | index;
        }

        public int GetCbufSlot()
        {
            return (Value >> AttrSlotLsb) & AttrSlotMask;
        }

        public int GetCbufOffset()
        {
            return Value & ~(AttrSlotMask << AttrSlotLsb);
        }

        public Register GetRegister()
        {
            return new Register(Value & 0xffffff, (RegisterType)(Value >> 24));
        }
    }
}