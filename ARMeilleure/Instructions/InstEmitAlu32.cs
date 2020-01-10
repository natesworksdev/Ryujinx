using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitAluHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit32
    {
        public static void Add(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context, setCarry: false);

            Operand res = context.Add(n, m);

            if (op.SetFlags)
            {
                EmitNZFlagsCheck(context, res);

                EmitAddsCCheck(context, n, res);
                EmitAddsVCheck(context, n, m, res);
            }

            EmitAluStore(context, res);
        }

        public static void Adc(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context, setCarry: false);

            Operand res = context.Add(n, m);

            Operand carry = GetFlag(PState.CFlag);

            res = context.Add(res, carry);

            if (op.SetFlags)
            {
                EmitNZFlagsCheck(context, res);

                EmitAdcsCCheck(context, n, res);
                EmitAddsVCheck(context, n, m, res);
            }

            EmitAluStore(context, res);
        }

        public static void Clz(ArmEmitterContext context)
        {
            IOpCode32AluReg op = (IOpCode32AluReg)context.CurrOp;

            Operand m = GetAluM(context, setCarry: false);

            Operand res = context.CountLeadingZeros(m);
            EmitAluStore(context, res);
        }

        public static void Cmp(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context, setCarry: false);

            Operand res = context.Subtract(n, m);

            EmitNZFlagsCheck(context, res);

            EmitSubsCCheck(context, n, res);
            EmitSubsVCheck(context, n, m, res);
        }

        public static void Cmn(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context, setCarry: false);

            Operand res = context.Add(n, m);

            EmitNZFlagsCheck(context, res);

            EmitAddsCCheck(context, n, res);
            EmitAddsVCheck(context, n, m, res);
        }

        public static void Mov(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand m = GetAluM(context);

            if (op.SetFlags)
            {
                EmitNZFlagsCheck(context, m);
            }

            EmitAluStore(context, m);
        }

        public static void Sub(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context, setCarry: false);

            Operand res = context.Subtract(n, m);

            if (op.SetFlags)
            {
                EmitNZFlagsCheck(context, res);

                EmitSubsCCheck(context, n, res);
                EmitSubsVCheck(context, n, m, res);
            }

            EmitAluStore(context, res);
        }

        public static void Sbc(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;
            Operand n = GetAluN(context);
            Operand m = GetAluM(context, setCarry: false);

            Operand res = context.Subtract(n, m);

            Operand borrow = context.BitwiseExclusiveOr(GetFlag(PState.CFlag), Const(1));

            res = context.Subtract(res, borrow);

            if (op.SetFlags)
            {
                EmitNZFlagsCheck(context, res);

                EmitSbcsCCheck(context, n, m);
                EmitSubsVCheck(context, n, m, res);
            }

            EmitAluStore(context, res);
        }

        public static void Rsc(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;
            Operand n = GetAluN(context);
            Operand m = GetAluM(context, setCarry: false);

            Operand res = context.Subtract(m, n);

            Operand borrow = context.BitwiseExclusiveOr(GetFlag(PState.CFlag), Const(1));

            res = context.Subtract(res, borrow);

            if (op.SetFlags)
            {
                EmitNZFlagsCheck(context, res);

                EmitSbcsCCheck(context, m, n);
                EmitSubsVCheck(context, m, n, res);
            }

            EmitAluStore(context, res);
        }

        public static void Rsb(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context, setCarry: false);

            Operand res = context.Subtract(m, n);

            if (op.SetFlags)
            {
                EmitNZFlagsCheck(context, res);

                EmitSubsCCheck(context, m, res);
                EmitSubsVCheck(context, m, n, res);
            }

            EmitAluStore(context, res);
        }

        public static void And(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand res = context.BitwiseAnd(n, m);

            if (op.SetFlags)
            {
                EmitNZFlagsCheck(context, res);
            }

            EmitAluStore(context, res);
        }

        public static void Bic(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand res = context.BitwiseAnd(n, context.BitwiseNot(m));

            if (op.SetFlags)
            {
                EmitNZFlagsCheck(context, res);
            }

            EmitAluStore(context, res);
        }

        public static void Tst(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand res = context.BitwiseAnd(n, m);
            EmitNZFlagsCheck(context, res);
        }

        public static void Orr(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand res = context.BitwiseOr(n, m);

            if (op.SetFlags)
            {
                EmitNZFlagsCheck(context, res);
            }

            EmitAluStore(context, res);
        }

        public static void Eor(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand res = context.BitwiseExclusiveOr(n, m);

            if (op.SetFlags)
            {
                EmitNZFlagsCheck(context, res);
            }

            EmitAluStore(context, res);
        }

        public static void Teq(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand res = context.BitwiseExclusiveOr(n, m);

            EmitNZFlagsCheck(context, res);
        }

        private static void EmitSignExtend(ArmEmitterContext context, bool signed, int bits)
        {
            IOpCode32AluUx op = (IOpCode32AluUx)context.CurrOp;

            Operand m = GetAluM(context);
            Operand res;

            if (op.RotateBits == 0)
            {
                res = m;
            }
            else
            {
                Operand rotate = Const(op.RotateBits);
                res = context.RotateRight(m, rotate);
            }

            switch (bits)
            {
                case 8:
                    res = (signed) ? context.SignExtend8(OperandType.I32, res) : context.ZeroExtend8(OperandType.I32, res);
                    break;
                case 16:
                    res = (signed) ? context.SignExtend16(OperandType.I32, res) : context.ZeroExtend16(OperandType.I32, res);
                    break;
            }
            

            if (op.Add)
            {
                res = context.Add(res, GetAluN(context));
            }

            EmitAluStore(context, res);
        }

        public static void Uxtb(ArmEmitterContext context)
        {
            EmitSignExtend(context, false, 8);
        }

        public static void Uxth(ArmEmitterContext context)
        {
            EmitSignExtend(context, false, 16);
        }

        public static void Sxtb(ArmEmitterContext context)
        {
            EmitSignExtend(context, true, 8);
        }

        public static void Sxth(ArmEmitterContext context)
        {
            EmitSignExtend(context, true, 16);
        }

        public static void Mla(ArmEmitterContext context)
        {
            OpCode32AluMla op = (OpCode32AluMla)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context);
            Operand a = GetIntA32(context, op.Ra);

            Operand res = context.Add(context.Multiply(n, m), a);

            if (op.SetFlags)
            {
                EmitNZFlagsCheck(context, res);
            }

            EmitAluStore(context, res);
        }

        public static void Mls(ArmEmitterContext context)
        {
            OpCode32AluMla op = (OpCode32AluMla)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context);
            Operand a = GetIntA32(context, op.Ra);

            Operand res = context.Subtract(a, context.Multiply(n, m));

            EmitAluStore(context, res);
        }

        public static void Udiv(ArmEmitterContext context)
        {
            EmitDiv(context, true);
        }

        public static void Sdiv(ArmEmitterContext context)
        {
            EmitDiv(context, false);
        }

        public static void EmitDiv(ArmEmitterContext context, bool unsigned)
        {
            OpCode32AluMla op = (OpCode32AluMla)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context);
            Operand zero = Const(m.Type, 0);

            Operand divisorIsZero = context.ICompareEqual(m, zero);

            Operand lblBadDiv = Label();
            Operand lblEnd = Label();

            context.BranchIfTrue(lblBadDiv, divisorIsZero);

            if (!unsigned)
            {
                // If Rn == INT_MIN && Rm == -1, Rd = INT_MIN (overflow).
                // assume this is the same as ARM64 for now - tests to follow.

                Operand intMin = Const(int.MinValue);
                Operand minus1 = Const(-1);

                Operand nIsIntMin = context.ICompareEqual(n, intMin);
                Operand mIsMinus1 = context.ICompareEqual(m, minus1);

                Operand lblGoodDiv = Label();

                context.BranchIfFalse(lblGoodDiv, context.BitwiseAnd(nIsIntMin, mIsMinus1));

                EmitAluStore(context, intMin);

                context.Branch(lblEnd);

                context.MarkLabel(lblGoodDiv);
            }

            Operand res = unsigned
                ? context.DivideUI(n, m)
                : context.Divide(n, m);

            EmitAluStore(context, res);

            context.Branch(lblEnd);

            context.MarkLabel(lblBadDiv);

            EmitAluStore(context, zero);

            context.MarkLabel(lblEnd);
        }

        public static void Movt(ArmEmitterContext context)
        {
            OpCode32AluImm16 op = (OpCode32AluImm16)context.CurrOp;

            Operand d = GetIntOrZR(context, op.Rd);
            Operand imm = Const(op.Immediate << 16); //immeditate value as top halfword
            Operand res = context.BitwiseAnd(d, Const(0x0000ffff));
            res = context.BitwiseOr(res, imm);

            EmitAluStore(context, res);
        }

        public static void Mvn(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;
            Operand m = GetAluM(context);

            Operand res = context.BitwiseNot(m);

            if (op.SetFlags)
            {
                EmitNZFlagsCheck(context, res);
            }

            EmitAluStore(context, res);
        }

        public static void Mul(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand res = context.Multiply(n, m);

            if (op.SetFlags)
            {
                EmitNZFlagsCheck(context, res);
            }

            EmitAluStore(context, res);
        }

        public static void Pkh(ArmEmitterContext context)
        {
            OpCode32AluRsImm op = (OpCode32AluRsImm)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand res = InstEmit.EmitReverseBits32Op(context, m);

            bool tbform = op.ShiftType == ShiftType.Asr;
            if (tbform)
            {
                res = context.BitwiseOr(context.BitwiseAnd(n, Const(0xFFFF0000)), context.BitwiseAnd(m, Const(0xFFFF)));
            }
            else
            {
                res = context.BitwiseOr(context.BitwiseAnd(m, Const(0xFFFF0000)), context.BitwiseAnd(n, Const(0xFFFF)));
            }

            EmitAluStore(context, res);
        }

        public static void Rbit(ArmEmitterContext context)
        {
            Operand m = GetAluM(context);
            Operand res = InstEmit.EmitReverseBits32Op(context, m);

            EmitAluStore(context, res);
        }

        public static void Rev(ArmEmitterContext context)
        {
            OpCode32Alu op = (OpCode32Alu)context.CurrOp;

            Operand m = GetAluM(context);

            Operand res = context.Call(new _U32_U32(System.Buffers.Binary.BinaryPrimitives.ReverseEndianness), m);

            EmitAluStore(context, res);
        }

        public static void Rev16(ArmEmitterContext context)
        {
            OpCode32Alu op = (OpCode32Alu)context.CurrOp;

            Operand m = GetAluM(context);

            Operand res = InstEmit.EmitReverseBytes16_32Op(context, m);

            EmitAluStore(context, res);
        }

        public static void Revsh(ArmEmitterContext context)
        {
            OpCode32Alu op = (OpCode32Alu)context.CurrOp;

            Operand m = GetAluM(context);

            Operand res = InstEmit.EmitReverseBytes16_32Op(context, m);

            EmitAluStore(context, context.SignExtend16(OperandType.I32, res));
        }

        public static void Bfc(ArmEmitterContext context)
        {
            OpCode32AluBf op = (OpCode32AluBf)context.CurrOp;

            Operand d = GetIntOrZR(context, op.Rd);
            Operand res = context.BitwiseAnd(d, Const(~op.DestMask));

            SetIntA32(context, op.Rd, res);
        }

        public static void Bfi(ArmEmitterContext context)
        {
            OpCode32AluBf op = (OpCode32AluBf)context.CurrOp;

            Operand n = GetIntOrZR(context, op.Rn);
            Operand d = GetIntOrZR(context, op.Rd);
            Operand part = context.BitwiseAnd(n, Const(op.SourceMask));
            if (op.Lsb != 0) part = context.ShiftLeft(part, Const(op.Lsb));
            Operand res = context.BitwiseAnd(d, Const(~op.DestMask));
            res = context.BitwiseOr(res, context.BitwiseAnd(part, Const(op.DestMask)));

            SetIntA32(context, op.Rd, res);
        }

        public static void Ubfx(ArmEmitterContext context)
        {
            OpCode32AluBf op = (OpCode32AluBf)context.CurrOp;

            var msb = op.Lsb + op.Msb; //for this instruction, the msb is actually a width
            var mask = (int)(0xFFFFFFFF >> (31 - msb)) << op.Lsb;

            Operand n = GetIntOrZR(context, op.Rn);
            Operand res = context.ShiftRightUI(context.ShiftLeft(n, Const(31 - msb)), Const(31 - op.Msb));

            SetIntA32(context, op.Rd, res);
        }

        public static void Sbfx(ArmEmitterContext context)
        {
            OpCode32AluBf op = (OpCode32AluBf)context.CurrOp;

            var msb = op.Lsb + op.Msb; //for this instruction, the msb is actually a width
            var mask = (int)(0xFFFFFFFF >> (31 - msb)) << op.Lsb;

            Operand n = GetIntOrZR(context, op.Rn);
            Operand res = context.ShiftRightSI(context.ShiftLeft(n, Const(31 - msb)), Const(31 - op.Msb));

            SetIntA32(context, op.Rd, res);
        }

        private static void EmitAluStore(ArmEmitterContext context, Operand value)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            if (op.Rd == RegisterAlias.Aarch32Pc)
            {
                if (op.SetFlags)
                {
                    // TODO: Load SPSR etc.
                    Operand isThumb = GetFlag(PState.TFlag);

                    Operand lblThumb = Label();

                    context.BranchIfTrue(lblThumb, isThumb);

                    context.Return(context.ZeroExtend32(OperandType.I64, context.BitwiseAnd(value, Const(~3))));

                    context.MarkLabel(lblThumb);

                    context.Return(context.ZeroExtend32(OperandType.I64, context.BitwiseAnd(value, Const(~1))));
                }
                else
                {
                    EmitAluWritePc(context, value);
                }
            }
            else
            {
                SetIntA32(context, op.Rd, value);
            }
        }

        private static void EmitAluWritePc(ArmEmitterContext context, Operand value)
        {
            context.StoreToContext();

            if (IsThumb(context.CurrOp))
            {
                context.Return(context.ZeroExtend32(OperandType.I64, context.BitwiseAnd(value, Const(~1))));
            }
            else
            {
                EmitBxWritePc(context, value);
            }
        }
    }
}