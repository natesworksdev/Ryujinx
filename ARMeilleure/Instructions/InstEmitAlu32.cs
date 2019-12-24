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

        public static void Uxtb(ArmEmitterContext context)
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

            res = context.ZeroExtend8(OperandType.I32, res);

            if (op.Add)
            {
                res = context.Add(res, GetAluN(context));
            }

            EmitAluStore(context, res);
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

        public static void Bfc(ArmEmitterContext context)
        {
            OpCode32AluBf op = (OpCode32AluBf)context.CurrOp;

            Operand d = GetIntOrZR(context, op.Rd);
            Operand res = context.BitwiseAnd(d, Const(~op.SourceMask));

            SetIntA32(context, op.Rd, res);
        }

        public static void Bfi(ArmEmitterContext context)
        {
            OpCode32AluBf op = (OpCode32AluBf)context.CurrOp;

            Operand n = GetIntOrZR(context, op.Rn);
            Operand d = GetIntOrZR(context, op.Rd);
            Operand part = context.BitwiseAnd(n, Const(op.SourceMask));
            Operand res = context.BitwiseAnd(d, Const(~op.SourceMask));
            res = context.BitwiseOr(res, part);

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