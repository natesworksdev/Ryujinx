using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using System;
using System.IO;

namespace ARMeilleure.CodeGen.X86
{
    class Assembler
    {
        private const int BadOp = 0;

        private const int OpModRMBits = 16;

        private const int R16Prefix = 0x66;

        private struct InstInfo
        {
            public int OpRMR     { get; }
            public int OpRMImm8  { get; }
            public int OpRMImm32 { get; }
            public int OpRImm64  { get; }
            public int OpRRM     { get; }
            public int Opers     { get; }

            public InstInfo(
                int opRMR,
                int opRMImm8,
                int opRMImm32,
                int opRImm64,
                int opRRM,
                int opers)
            {
                OpRMR     = opRMR;
                OpRMImm8  = opRMImm8;
                OpRMImm32 = opRMImm32;
                OpRImm64  = opRImm64;
                OpRRM     = opRRM;
                Opers     = opers;
            }
        }

        private static InstInfo[] _instTable;

        private Stream _stream;

        static Assembler()
        {
            _instTable = new InstInfo[(int)X86Instruction.Count];

            //  Name                                 RM/R      RM/I8     RM/I32    R/I64     R/RM      Opers
            Add(X86Instruction.Add,     new InstInfo(0x000001, 0x000083, 0x000081, BadOp,    0x000003, 2));
            Add(X86Instruction.And,     new InstInfo(0x000021, 0x040083, 0x040081, BadOp,    0x000023, 2));
            Add(X86Instruction.Cmp,     new InstInfo(0x000039, 0x070083, 0x070081, BadOp,    0x00003b, 2));
            Add(X86Instruction.Idiv,    new InstInfo(BadOp,    BadOp,    BadOp,    BadOp,    0x0700f7, 1));
            Add(X86Instruction.Imul,    new InstInfo(BadOp,    0x00006b, 0x000069, BadOp,    0x000faf, 2));
            Add(X86Instruction.Mov,     new InstInfo(0x000089, BadOp,    0x0000c7, 0x0000b8, 0x00008b, 2));
            Add(X86Instruction.Mov16,   new InstInfo(0x000089, BadOp,    0x0000c7, BadOp,    0x00008b, 2));
            Add(X86Instruction.Mov8,    new InstInfo(0x000088, 0x0000c6, BadOp,    BadOp,    0x00008a, 2));
            Add(X86Instruction.Movsx16, new InstInfo(BadOp,    BadOp,    BadOp,    BadOp,    0x000fbf, 2));
            Add(X86Instruction.Movsx32, new InstInfo(BadOp,    BadOp,    BadOp,    BadOp,    0x000063, 2));
            Add(X86Instruction.Movsx8,  new InstInfo(BadOp,    BadOp,    BadOp,    BadOp,    0x000fbe, 2));
            Add(X86Instruction.Movzx16, new InstInfo(BadOp,    BadOp,    BadOp,    BadOp,    0x000fb7, 2));
            Add(X86Instruction.Movzx8,  new InstInfo(BadOp,    BadOp,    BadOp,    BadOp,    0x000fb6, 2));
            Add(X86Instruction.Neg,     new InstInfo(0x0300f7, BadOp,    BadOp,    BadOp,    BadOp,    1));
            Add(X86Instruction.Not,     new InstInfo(0x0200f7, BadOp,    BadOp,    BadOp,    BadOp,    1));
            Add(X86Instruction.Or,      new InstInfo(0x000009, 0x010083, 0x010081, BadOp,    0x00000b, 2));
            Add(X86Instruction.Pop,     new InstInfo(0x00008f, BadOp,    BadOp,    BadOp,    BadOp,    1));
            Add(X86Instruction.Push,    new InstInfo(BadOp,    0x00006a, 0x000068, BadOp,    0x0600ff, 1));
            Add(X86Instruction.Ror,     new InstInfo(0x0100d3, 0x0100c1, BadOp,    BadOp,    BadOp,    2));
            Add(X86Instruction.Sar,     new InstInfo(0x0700d3, 0x0700c1, BadOp,    BadOp,    BadOp,    2));
            Add(X86Instruction.Shl,     new InstInfo(0x0400d3, 0x0400c1, BadOp,    BadOp,    BadOp,    2));
            Add(X86Instruction.Shr,     new InstInfo(0x0500d3, 0x0500c1, BadOp,    BadOp,    BadOp,    2));
            Add(X86Instruction.Sub,     new InstInfo(0x000029, 0x050083, 0x050081, BadOp,    0x00002b, 2));
            Add(X86Instruction.Test,    new InstInfo(0x000085, BadOp,    0x0000f7, BadOp,    BadOp,    2));
            Add(X86Instruction.Xor,     new InstInfo(0x000031, 0x060083, 0x060081, BadOp,    0x000033, 2));
        }

        private static void Add(X86Instruction inst, InstInfo info)
        {
            _instTable[(int)inst] = info;
        }

        public Assembler(Stream stream)
        {
            _stream = stream;
        }

        public void Add(Operand dest, Operand source)
        {
            WriteInstruction(dest, source, X86Instruction.Add);
        }

        public void And(Operand dest, Operand source)
        {
            WriteInstruction(dest, source, X86Instruction.And);
        }

        public void Cdq()
        {
            WriteByte(0x99);
        }

        public void Cmovcc(Operand dest, Operand source, X86Condition condition)
        {
            WriteRRMOpCode(dest, source, 0x0f40 | (int)condition);
        }

        public void Cmp(Operand src1, Operand src2)
        {
            WriteInstruction(src1, src2, X86Instruction.Cmp);
        }

        public void Cqo()
        {
            WriteByte(0x48);
            WriteByte(0x99);
        }

        public void Idiv(Operand source)
        {
            WriteInstruction(null, source, X86Instruction.Idiv);
        }

        public void Imul(Operand dest, Operand source)
        {
            if (source.Kind != OperandKind.Register)
            {
                throw new ArgumentException($"Invalid source operand kind \"{source.Kind}\".");
            }

            WriteInstruction(dest, source, X86Instruction.Imul);
        }

        public void Imul(Operand dest, Operand src1, Operand src2)
        {
            InstInfo info = _instTable[(int)X86Instruction.Imul];

            if (src2.Kind != OperandKind.Constant)
            {
                throw new ArgumentException($"Invalid source 2 operand kind \"{src2.Kind}\".");
            }

            if (IsImm8(src2) && info.OpRMImm8 != BadOp)
            {
                WriteRRMOpCode(dest, src1, info.OpRMImm8);

                WriteByte(src2.AsInt32());
            }
            else if (IsImm32(src2) && info.OpRMImm32 != BadOp)
            {
                WriteRRMOpCode(dest, src1, info.OpRMImm32);

                WriteInt32(src2.AsInt32());
            }
            else
            {
                throw new ArgumentException($"Failed to encode constant 0x{src2.Value:X}.");
            }
        }

        public void Jcc(X86Condition condition, long offset)
        {
            if (ConstFitsOnS8(offset))
            {
                WriteByte(0x70 | (int)condition);

                WriteByte((byte)offset);
            }
            else if (ConstFitsOnS32(offset))
            {
                WriteByte(0x0f);
                WriteByte(0x80 | (int)condition);

                WriteInt32((int)offset);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
        }

        public void Jmp(long offset)
        {
            if (ConstFitsOnS8(offset))
            {
                WriteByte(0xeb);

                WriteByte((byte)offset);
            }
            else if (ConstFitsOnS32(offset))
            {
                WriteByte(0xe9);

                WriteInt32((int)offset);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
        }

        public void Mov(Operand dest, Operand source)
        {
            WriteInstruction(dest, source, X86Instruction.Mov);
        }

        public void Mov16(Operand dest, Operand source)
        {
            WriteByte(R16Prefix);

            WriteInstruction(dest, source, X86Instruction.Mov16);
        }

        public void Mov8(Operand dest, Operand source)
        {
            WriteInstruction(dest, source, X86Instruction.Mov8);
        }

        public void Movsx16(Operand dest, Operand source)
        {
            WriteInstruction(dest, source, X86Instruction.Movsx16);
        }

        public void Movsx32(Operand dest, Operand source)
        {
            WriteInstruction(dest, source, X86Instruction.Movsx32);
        }

        public void Movsx8(Operand dest, Operand source)
        {
            WriteInstruction(dest, source, X86Instruction.Movsx8);
        }

        public void Movzx16(Operand dest, Operand source)
        {
            WriteInstruction(dest, source, X86Instruction.Movzx16);
        }

        public void Movzx8(Operand dest, Operand source)
        {
            WriteInstruction(dest, source, X86Instruction.Movzx8);
        }

        public void Neg(Operand dest)
        {
            WriteInstruction(dest, null, X86Instruction.Neg);
        }

        public void Not(Operand dest)
        {
            WriteInstruction(dest, null, X86Instruction.Not);
        }

        public void Or(Operand dest, Operand source)
        {
            WriteInstruction(dest, source, X86Instruction.Or);
        }

        public void Pop(Operand dest)
        {
            if (dest.Kind == OperandKind.Register)
            {
                WriteCompactInst(dest, 0x58);
            }
            else
            {
                WriteInstruction(dest, null, X86Instruction.Pop);
            }
        }

        public void Push(Operand source)
        {
            if (source.Kind == OperandKind.Register)
            {
                WriteCompactInst(source, 0x50);
            }
            else
            {
                WriteInstruction(null, source, X86Instruction.Push);
            }
        }

        public void Return()
        {
            WriteByte(0xc3);
        }

        public void Ror(Operand dest, Operand source)
        {
            WriteShiftInst(dest, source, X86Instruction.Ror);
        }

        public void Sar(Operand dest, Operand source)
        {
            WriteShiftInst(dest, source, X86Instruction.Sar);
        }

        public void Shl(Operand dest, Operand source)
        {
            WriteShiftInst(dest, source, X86Instruction.Shl);
        }

        public void Shr(Operand dest, Operand source)
        {
            WriteShiftInst(dest, source, X86Instruction.Shr);
        }

        public void Setcc(Operand dest, X86Condition condition)
        {
            WriteOpCode(dest, null, 0x0f90 | (int)condition, rrm: false, r8h: true);
        }

        public void Sub(Operand dest, Operand source)
        {
            WriteInstruction(dest, source, X86Instruction.Sub);
        }

        public void Test(Operand src1, Operand src2)
        {
            WriteInstruction(src1, src2, X86Instruction.Test);
        }

        public void Xor(Operand dest, Operand source)
        {
            WriteInstruction(dest, source, X86Instruction.Xor);
        }

        private void WriteShiftInst(Operand dest, Operand source, X86Instruction inst)
        {
            if (source.Kind == OperandKind.Register)
            {
                X86Register shiftReg = (X86Register)source.GetRegister().Index;

                if (shiftReg != X86Register.Rcx)
                {
                    throw new ArgumentException($"Invalid shift register \"{shiftReg}\".");
                }
            }

            WriteInstruction(dest, source, inst);
        }

        private void WriteInstruction(Operand dest, Operand source, X86Instruction inst)
        {
            InstInfo info = _instTable[(int)inst];

            if (source != null)
            {
                if (source.Kind == OperandKind.Constant)
                {
                    if (inst == X86Instruction.Mov8)
                    {
                        WriteOpCode(dest, null, info.OpRMImm8);

                        WriteByte(source.AsInt32());
                    }
                    else if (inst == X86Instruction.Mov16)
                    {
                        WriteOpCode(dest, null, info.OpRMImm32);

                        WriteInt16(source.AsInt16());
                    }
                    else if (IsImm8(source) && info.OpRMImm8 != BadOp)
                    {
                        WriteOpCode(dest, null, info.OpRMImm8);

                        WriteByte(source.AsInt32());
                    }
                    else if (IsImm32(source) && info.OpRMImm32 != BadOp)
                    {
                        WriteOpCode(dest, null, info.OpRMImm32);

                        WriteInt32(source.AsInt32());
                    }
                    else if (dest != null && IsR64(dest) && info.OpRImm64 != BadOp)
                    {
                        int rexPrefix = GetRexPrefix(dest, source, rrm: false);

                        if (rexPrefix != 0)
                        {
                            WriteByte(rexPrefix);
                        }

                        WriteByte(info.OpRImm64 + (dest.GetRegister().Index & 0b111));

                        WriteUInt64(source.Value);
                    }
                    else
                    {
                        throw new ArgumentException($"Failed to encode constant 0x{source.Value:X}.");
                    }
                }
                else if (source.Kind == OperandKind.Register && info.OpRMR != BadOp)
                {
                    WriteOpCode(dest, source, info.OpRMR);
                }
                else if (info.OpRRM != BadOp)
                {
                    WriteRRMOpCode(dest, source, info.OpRRM);
                }
                else
                {
                    throw new ArgumentException($"Invalid source operand kind \"{source.Kind}\".");
                }
            }
            else if (info.Opers == 1)
            {
                WriteOpCode(dest, source, info.OpRMR);
            }
            else
            {
                throw new ArgumentNullException(nameof(source));
            }
        }

        private void WriteRRMOpCode(Operand dest, Operand source, int opCode)
        {
            WriteOpCode(dest, source, opCode, rrm: true);
        }

        private void WriteOpCode(
            Operand dest,
            Operand source,
            int     opCode,
            bool    rrm = false,
            bool    r8h = false)
        {
            int rexPrefix = GetRexPrefix(dest, source, rrm);

            int modRM = (opCode >> OpModRMBits) << 3;

            X86MemoryOperand memOp = null;

            if (dest != null)
            {
                if (dest.Kind == OperandKind.Register)
                {
                    int regIndex = dest.GetRegister().Index;

                    modRM |= (regIndex & 0b111) << (rrm ? 3 : 0);

                    if (r8h && regIndex >= 4)
                    {
                        rexPrefix |= 0x40;
                    }
                }
                else if (dest.Kind == OperandKind.Memory)
                {
                    memOp = (X86MemoryOperand)dest;
                }
                else
                {
                    throw new ArgumentException("Invalid destination operand kind \"" + dest.Kind + "\".");
                }
            }

            if (source != null)
            {
                if (source.Kind == OperandKind.Register)
                {
                    modRM |= (source.GetRegister().Index & 0b111) << (rrm ? 0 : 3);
                }
                else if (source.Kind == OperandKind.Memory && memOp == null)
                {
                    memOp = (X86MemoryOperand)source;
                }
                else
                {
                    throw new ArgumentException("Invalid source operand kind \"" + source.Kind + "\".");
                }
            }

            bool needsSibByte = false;

            bool needsDisplacement = false;

            int sib = 0;

            if (memOp != null)
            {
                //Either source or destination is a memory operand.
                Register baseReg = memOp.BaseAddress.GetRegister();

                X86Register baseRegLow = (X86Register)(baseReg.Index & 0b111);

                needsSibByte = memOp.Index != null || baseRegLow == X86Register.Rsp;

                needsDisplacement = memOp.Displacement != 0 || baseRegLow == X86Register.Rbp;

                if (needsDisplacement)
                {
                    if (ConstFitsOnS8(memOp.Displacement))
                    {
                        modRM |= 0x40;
                    }
                    else /* if (ConstFitsOnS32(memOp.Displacement)) */
                    {
                        modRM |= 0x80;
                    }
                }

                if (needsSibByte)
                {
                    if (baseReg.Index >= 8)
                    {
                        rexPrefix |= 0x40 | (baseReg.Index >> 3);
                    }

                    sib = (int)baseRegLow;

                    if (memOp.Index != null)
                    {
                        int indexReg = memOp.Index.GetRegister().Index;

                        if (indexReg == (int)X86Register.Rsp)
                        {
                            throw new ArgumentException("Using RSP as index register on the memory operand is not allowed.");
                        }

                        if (indexReg >= 8)
                        {
                            rexPrefix |= 0x40 | (indexReg >> 3) << 1;
                        }

                        sib |= (indexReg & 0b111) << 3;
                    }
                    else
                    {
                        sib |= 0b100 << 3;
                    }

                    sib |= (int)memOp.Scale << 6;

                    modRM |= 0b100;
                }
                else
                {
                    modRM |= (int)baseRegLow;
                }
            }
            else
            {
                //Source and destination are registers.
                modRM |= 0xc0;
            }

            if (rexPrefix != 0)
            {
                WriteByte(rexPrefix);
            }

            if ((opCode & 0xff00) != 0)
            {
                WriteByte(opCode >> 8);
            }

            WriteByte(opCode);
            WriteByte(modRM);

            if (needsSibByte)
            {
                WriteByte(sib);
            }

            if (needsDisplacement)
            {
                if (ConstFitsOnS8(memOp.Displacement))
                {
                    WriteByte(memOp.Displacement);
                }
                else /* if (ConstFitsOnS32(memOp.Displacement)) */
                {
                    WriteInt32(memOp.Displacement);
                }
            }
        }

        private void WriteCompactInst(Operand operand, int opCode)
        {
            int regIndex = operand.GetRegister().Index;

            if (regIndex >= 8)
            {
                WriteByte(0x41);
            }

            WriteByte(opCode + (regIndex & 0b111));
        }

        private static int GetRexPrefix(Operand dest, Operand source, bool rrm)
        {
            int rexPrefix = 0;

            void SetRegisterHighBit(Register reg, int bit)
            {
                if (reg.Index >= 8)
                {
                    rexPrefix |= 0x40 | (reg.Index >> 3) << bit;
                }
            }

            if (dest != null)
            {
                if (dest.Type == OperandType.I64)
                {
                    rexPrefix = 0x48;
                }

                if (dest.Kind == OperandKind.Register)
                {
                    SetRegisterHighBit(dest.GetRegister(), (rrm ? 2 : 0));
                }
            }

            if (source != null && source.Kind == OperandKind.Register)
            {
                SetRegisterHighBit(source.GetRegister(), (rrm ? 0 : 2));
            }

            return rexPrefix;
        }

        private static bool IsR64(Operand operand)
        {
            return operand.Kind == OperandKind.Register &&
                   operand.Type == OperandType.I64;
        }

        private static bool IsImm8(Operand operand)
        {
            long value = operand.Type == OperandType.I32 ? operand.AsInt32()
                                                         : operand.AsInt64();

            return ConstFitsOnS8(value);
        }

        private static bool IsImm32(Operand operand)
        {
            long value = operand.Type == OperandType.I32 ? operand.AsInt32()
                                                         : operand.AsInt64();

            return ConstFitsOnS32(value);
        }

        public static int GetJccLength(long offset)
        {
            if (ConstFitsOnS8(offset < 0 ? offset - 2 : offset))
            {
                return 2;
            }
            else if (ConstFitsOnS32(offset < 0 ? offset - 6 : offset))
            {
                return 6;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
        }

        public static int GetJmpLength(long offset)
        {
            if (ConstFitsOnS8(offset < 0 ? offset - 2 : offset))
            {
                return 2;
            }
            else if (ConstFitsOnS32(offset < 0 ? offset - 5 : offset))
            {
                return 5;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
        }

        private static bool ConstFitsOnU32(long value)
        {
            return value >> 32 == 0;
        }

        private static bool ConstFitsOnS8(long value)
        {
            return value == (sbyte)value;
        }

        private static bool ConstFitsOnS32(long value)
        {
            return value == (int)value;
        }

        private void WriteInt16(short value)
        {
            WriteUInt16((ushort)value);
        }

        private void WriteInt32(int value)
        {
            WriteUInt32((uint)value);
        }

        private void WriteInt64(long value)
        {
            WriteUInt64((ulong)value);
        }

        private void WriteUInt16(ushort value)
        {
            _stream.WriteByte((byte)(value >> 0));
            _stream.WriteByte((byte)(value >> 8));
        }

        private void WriteUInt32(uint value)
        {
            _stream.WriteByte((byte)(value >> 0));
            _stream.WriteByte((byte)(value >> 8));
            _stream.WriteByte((byte)(value >> 16));
            _stream.WriteByte((byte)(value >> 24));
        }

        private void WriteUInt64(ulong value)
        {
            _stream.WriteByte((byte)(value >> 0));
            _stream.WriteByte((byte)(value >> 8));
            _stream.WriteByte((byte)(value >> 16));
            _stream.WriteByte((byte)(value >> 24));
            _stream.WriteByte((byte)(value >> 32));
            _stream.WriteByte((byte)(value >> 40));
            _stream.WriteByte((byte)(value >> 48));
            _stream.WriteByte((byte)(value >> 56));
        }

        private void WriteByte(int value)
        {
            _stream.WriteByte((byte)value);
        }
    }
}