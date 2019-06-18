// https://github.com/intel/ARM_NEON_2_x86_SSE/blob/master/NEON_2_SSE.h

using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Reflection;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    using Func2I = Func<Operand, Operand, Operand>;

    static partial class InstEmit
    {
#region "Masks"
        private static readonly long[] _masks_RshrnShrn = new long[]
        {
            14L << 56 | 12L << 48 | 10L << 40 | 08L << 32 | 06L << 24 | 04L << 16 | 02L << 8 | 00L << 0,
            13L << 56 | 12L << 48 | 09L << 40 | 08L << 32 | 05L << 24 | 04L << 16 | 01L << 8 | 00L << 0,
            11L << 56 | 10L << 48 | 09L << 40 | 08L << 32 | 03L << 24 | 02L << 16 | 01L << 8 | 00L << 0
        };
#endregion

        public static void Rshrn_V(EmitterContext context)
        {
            if (Optimizations.UseSsse3)
            {
                OpCodeSimdShImm op = (OpCodeSimdShImm)context.CurrOp;

                int shift = GetImmShr(op);

                long roundConst = 1L << (shift - 1);

                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);

                Operand dLow = context.AddIntrinsic(Instruction.X86Movlhps, d, context.VectorZero());

                Operand mask = null;

                switch (op.Size + 1)
                {
                    case 1: mask = X86GetAllElements(context, (int)roundConst * 0x00010001); break;
                    case 2: mask = X86GetAllElements(context, (int)roundConst); break;
                    case 3: mask = X86GetAllElements(context,      roundConst); break;
                }

                Instruction addInst = X86PaddInstruction[op.Size + 1];

                Operand res = context.AddIntrinsic(addInst, n, mask);

                Instruction srlInst = X86PsrlInstruction[op.Size + 1];

                res = context.AddIntrinsic(srlInst, res, Const(shift));

                Operand mask2 = X86GetAllElements(context, _masks_RshrnShrn[op.Size]);

                res = context.AddIntrinsic(Instruction.X86Pshufb, res, mask2);

                Instruction movInst = op.RegisterSize == RegisterSize.Simd128
                    ? Instruction.X86Movlhps
                    : Instruction.X86Movhlps;

                res = context.AddIntrinsic(movInst, dLow, res);

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorShrImmNarrowOpZx(context, round: true);
            }
        }

        public static void Shl_S(EmitterContext context)
        {
            OpCodeSimdShImm op = (OpCodeSimdShImm)context.CurrOp;

            int shift = GetImmShl(op);

            EmitScalarUnaryOpZx(context, (op1) => context.ShiftLeft(op1, Const(shift)));
        }

        public static void Shl_V(EmitterContext context)
        {
            OpCodeSimdShImm op = (OpCodeSimdShImm)context.CurrOp;

            int shift = GetImmShl(op);

            if (Optimizations.UseSse2 && op.Size > 0)
            {
                Operand n = GetVec(op.Rn);

                Instruction sllInst = X86PsllInstruction[op.Size];

                Operand res = context.AddIntrinsic(sllInst, n, Const(shift));

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorUnaryOpZx(context, (op1) =>  context.ShiftLeft(op1, Const(shift)));
            }
        }

        public static void Shll_V(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            int shift = 8 << op.Size;

            if (Optimizations.UseSse41)
            {
                Operand n = GetVec(op.Rn);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Instruction.X86Psrldq, n, Const(8));
                }

                Instruction movsxInst = X86PmovsxInstruction[op.Size];

                Operand res = context.AddIntrinsic(movsxInst, n);

                Instruction sllInst = X86PsllInstruction[op.Size + 1];

                res = context.AddIntrinsic(sllInst, res, Const(shift));

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorShImmWidenBinaryZx(context, (op1, op2) => context.ShiftLeft(op1, op2), shift);
            }
        }

        public static void Shrn_V(EmitterContext context)
        {
            if (Optimizations.UseSsse3)
            {
                OpCodeSimdShImm op = (OpCodeSimdShImm)context.CurrOp;

                int shift = GetImmShr(op);

                long roundConst = 1L << (shift - 1);

                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);

                Operand dLow = context.AddIntrinsic(Instruction.X86Movlhps, d, context.VectorZero());

                Instruction srlInst = X86PsrlInstruction[op.Size + 1];

                Operand nShifted = context.AddIntrinsic(srlInst, n, Const(shift));

                Operand mask = X86GetAllElements(context, _masks_RshrnShrn[op.Size]);

                Operand res = context.AddIntrinsic(Instruction.X86Pshufb, nShifted, mask);

                Instruction movInst = op.RegisterSize == RegisterSize.Simd128
                    ? Instruction.X86Movlhps
                    : Instruction.X86Movhlps;

                res = context.AddIntrinsic(movInst, dLow, res);

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorShrImmNarrowOpZx(context, round: false);
            }
        }

        public static void Sli_V(EmitterContext context)
        {
            OpCodeSimdShImm op = (OpCodeSimdShImm)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            int shift = GetImmShl(op);

            ulong mask = shift != 0 ? ulong.MaxValue >> (64 - shift) : 0;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractZx(context, op.Rn, index, op.Size);

                Operand neShifted = context.ShiftLeft(ne, Const(shift));

                Operand de = EmitVectorExtractZx(context, op.Rd, index, op.Size);

                Operand deMasked = context.BitwiseAnd(de, Const(mask));

                Operand e = context.BitwiseOr(neShifted, deMasked);

                res = EmitVectorInsert(context, res, e, index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Sqrshl_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractSx(context, op.Rn, index, op.Size);
                Operand me = EmitVectorExtractSx(context, op.Rm, index, op.Size);

                MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.SignedShlRegSatQ));

                Operand e = context.Call(info, ne, me, Const(1), Const(op.Size));

                res = EmitVectorInsert(context, res, e, index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Sqrshrn_S(EmitterContext context)
        {
            EmitRoundShrImmSaturatingNarrowOp(context, ShrImmSaturatingNarrowFlags.ScalarSxSx);
        }

        public static void Sqrshrn_V(EmitterContext context)
        {
            EmitRoundShrImmSaturatingNarrowOp(context, ShrImmSaturatingNarrowFlags.VectorSxSx);
        }

        public static void Sqrshrun_S(EmitterContext context)
        {
            EmitRoundShrImmSaturatingNarrowOp(context, ShrImmSaturatingNarrowFlags.ScalarSxZx);
        }

        public static void Sqrshrun_V(EmitterContext context)
        {
            EmitRoundShrImmSaturatingNarrowOp(context, ShrImmSaturatingNarrowFlags.VectorSxZx);
        }

        public static void Sqshl_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractSx(context, op.Rn, index, op.Size);
                Operand me = EmitVectorExtractSx(context, op.Rm, index, op.Size);

                MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.SignedShlRegSatQ));

                Operand e = context.Call(info, ne, me, Const(0), Const(op.Size));

                res = EmitVectorInsert(context, res, e, index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Sqshrn_S(EmitterContext context)
        {
            EmitShrImmSaturatingNarrowOp(context, ShrImmSaturatingNarrowFlags.ScalarSxSx);
        }

        public static void Sqshrn_V(EmitterContext context)
        {
            EmitShrImmSaturatingNarrowOp(context, ShrImmSaturatingNarrowFlags.VectorSxSx);
        }

        public static void Sqshrun_S(EmitterContext context)
        {
            EmitShrImmSaturatingNarrowOp(context, ShrImmSaturatingNarrowFlags.ScalarSxZx);
        }

        public static void Sqshrun_V(EmitterContext context)
        {
            EmitShrImmSaturatingNarrowOp(context, ShrImmSaturatingNarrowFlags.VectorSxZx);
        }

        public static void Srshl_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractSx(context, op.Rn, index, op.Size);
                Operand me = EmitVectorExtractSx(context, op.Rm, index, op.Size);

                MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.SignedShlReg));

                Operand e = context.Call(info, ne, me, Const(1), Const(op.Size));

                res = EmitVectorInsert(context, res, e, index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Srshr_S(EmitterContext context)
        {
            EmitScalarShrImmOpSx(context, ShrImmFlags.Round);
        }

        public static void Srshr_V(EmitterContext context)
        {
            OpCodeSimdShImm op = (OpCodeSimdShImm)context.CurrOp;

            if (Optimizations.UseSse2 && op.Size > 0 && op.Size < 3)
            {
                int shift = GetImmShr(op);
                int eSize = 8 << op.Size;

                Operand n = GetVec(op.Rn);

                Instruction sllInst = X86PsllInstruction[op.Size];

                Operand res = context.AddIntrinsic(sllInst, n, Const(eSize - shift));

                Instruction srlInst = X86PsrlInstruction[op.Size];

                res = context.AddIntrinsic(srlInst, res, Const(eSize - 1));

                Instruction sraInst = X86PsraInstruction[op.Size];

                Operand nSra = context.AddIntrinsic(sraInst, n, Const(shift));

                Instruction addInst = X86PaddInstruction[op.Size];

                res = context.AddIntrinsic(addInst, res, nSra);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorShrImmOpSx(context, ShrImmFlags.Round);
            }
        }

        public static void Srsra_S(EmitterContext context)
        {
            EmitScalarShrImmOpSx(context, ShrImmFlags.Round | ShrImmFlags.Accumulate);
        }

        public static void Srsra_V(EmitterContext context)
        {
            OpCodeSimdShImm op = (OpCodeSimdShImm)context.CurrOp;

            if (Optimizations.UseSse2 && op.Size > 0 && op.Size < 3)
            {
                int shift = GetImmShr(op);
                int eSize = 8 << op.Size;

                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);

                Instruction sllInst = X86PsllInstruction[op.Size];

                Operand res = context.AddIntrinsic(sllInst, n, Const(eSize - shift));

                Instruction srlInst = X86PsrlInstruction[op.Size];

                res = context.AddIntrinsic(srlInst, res, Const(eSize - 1));

                Instruction sraInst = X86PsraInstruction[op.Size];

                Operand nSra = context.AddIntrinsic(sraInst, n, Const(shift));

                Instruction addInst = X86PaddInstruction[op.Size];

                res = context.AddIntrinsic(addInst, res, nSra);
                res = context.AddIntrinsic(addInst, res, d);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorShrImmOpSx(context, ShrImmFlags.Round | ShrImmFlags.Accumulate);
            }
        }

        public static void Sshl_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractSx(context, op.Rn, index, op.Size);
                Operand me = EmitVectorExtractSx(context, op.Rm, index, op.Size);

                MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.SignedShlReg));

                Operand e = context.Call(info, ne, me, Const(0), Const(op.Size));

                res = EmitVectorInsert(context, res, e, index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Sshll_V(EmitterContext context)
        {
            OpCodeSimdShImm op = (OpCodeSimdShImm)context.CurrOp;

            int shift = GetImmShl(op);

            if (Optimizations.UseSse41)
            {
                Operand n = GetVec(op.Rn);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Instruction.X86Psrldq, n, Const(8));
                }

                Instruction movsxInst = X86PmovsxInstruction[op.Size];

                Operand res = context.AddIntrinsic(movsxInst, n);

                if (shift != 0)
                {
                    Instruction sllInst = X86PsllInstruction[op.Size + 1];

                    res = context.AddIntrinsic(sllInst, res, Const(shift));
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorShImmWidenBinarySx(context, (op1, op2) => context.ShiftLeft(op1, op2), shift);
            }
        }

        public static void Sshr_S(EmitterContext context)
        {
            EmitShrImmOp(context, ShrImmFlags.ScalarSx);
        }

        public static void Sshr_V(EmitterContext context)
        {
            OpCodeSimdShImm op = (OpCodeSimdShImm)context.CurrOp;

            if (Optimizations.UseSse2 && op.Size > 0 && op.Size < 3)
            {
                int shift = GetImmShr(op);

                Operand n = GetVec(op.Rn);

                Instruction sraInst = X86PsraInstruction[op.Size];

                Operand res = context.AddIntrinsic(sraInst, n, Const(shift));

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitShrImmOp(context, ShrImmFlags.VectorSx);
            }
        }

        public static void Ssra_S(EmitterContext context)
        {
            EmitScalarShrImmOpSx(context, ShrImmFlags.Accumulate);
        }

        public static void Ssra_V(EmitterContext context)
        {
            OpCodeSimdShImm op = (OpCodeSimdShImm)context.CurrOp;

            if (Optimizations.UseSse2 && op.Size > 0 && op.Size < 3)
            {
                int shift = GetImmShr(op);

                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);

                Instruction sraInst = X86PsraInstruction[op.Size];

                Operand res = context.AddIntrinsic(sraInst, n, Const(shift));

                Instruction addInst = X86PaddInstruction[op.Size];

                res = context.AddIntrinsic(addInst, res, d);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(d, res);
            }
            else
            {
                EmitVectorShrImmOpSx(context, ShrImmFlags.Accumulate);
            }
        }

        public static void Uqrshl_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractZx(context, op.Rn, index, op.Size);
                Operand me = EmitVectorExtractZx(context, op.Rm, index, op.Size);

                MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.UnsignedShlRegSatQ));

                Operand e = context.Call(info, ne, me, Const(1), Const(op.Size));

                res = EmitVectorInsert(context, res, e, index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Uqrshrn_S(EmitterContext context)
        {
            EmitRoundShrImmSaturatingNarrowOp(context, ShrImmSaturatingNarrowFlags.ScalarZxZx);
        }

        public static void Uqrshrn_V(EmitterContext context)
        {
            EmitRoundShrImmSaturatingNarrowOp(context, ShrImmSaturatingNarrowFlags.VectorZxZx);
        }

        public static void Uqshl_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractZx(context, op.Rn, index, op.Size);
                Operand me = EmitVectorExtractZx(context, op.Rm, index, op.Size);

                MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.UnsignedShlRegSatQ));

                Operand e = context.Call(info, ne, me, Const(0), Const(op.Size));

                res = EmitVectorInsert(context, res, e, index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Uqshrn_S(EmitterContext context)
        {
            EmitShrImmSaturatingNarrowOp(context, ShrImmSaturatingNarrowFlags.ScalarZxZx);
        }

        public static void Uqshrn_V(EmitterContext context)
        {
            EmitShrImmSaturatingNarrowOp(context, ShrImmSaturatingNarrowFlags.VectorZxZx);
        }

        public static void Urshl_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractZx(context, op.Rn, index, op.Size);
                Operand me = EmitVectorExtractZx(context, op.Rm, index, op.Size);

                MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.UnsignedShlReg));

                Operand e = context.Call(info, ne, me, Const(1), Const(op.Size));

                res = EmitVectorInsert(context, res, e, index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Urshr_S(EmitterContext context)
        {
            EmitScalarShrImmOpZx(context, ShrImmFlags.Round);
        }

        public static void Urshr_V(EmitterContext context)
        {
            OpCodeSimdShImm op = (OpCodeSimdShImm)context.CurrOp;

            if (Optimizations.UseSse2 && op.Size > 0)
            {
                int shift = GetImmShr(op);
                int eSize = 8 << op.Size;

                Operand n = GetVec(op.Rn);

                Instruction sllInst = X86PsllInstruction[op.Size];

                Operand res = context.AddIntrinsic(sllInst, n, Const(eSize - shift));

                Instruction srlInst = X86PsrlInstruction[op.Size];

                res = context.AddIntrinsic(srlInst, res, Const(eSize - 1));

                Operand nSrl = context.AddIntrinsic(srlInst, n, Const(shift));

                Instruction addInst = X86PaddInstruction[op.Size];

                res = context.AddIntrinsic(addInst, res, nSrl);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorShrImmOpZx(context, ShrImmFlags.Round);
            }
        }

        public static void Ursra_S(EmitterContext context)
        {
            EmitScalarShrImmOpZx(context, ShrImmFlags.Round | ShrImmFlags.Accumulate);
        }

        public static void Ursra_V(EmitterContext context)
        {
            OpCodeSimdShImm op = (OpCodeSimdShImm)context.CurrOp;

            if (Optimizations.UseSse2 && op.Size > 0)
            {
                int shift = GetImmShr(op);
                int eSize = 8 << op.Size;

                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);

                Instruction sllInst = X86PsllInstruction[op.Size];

                Operand res = context.AddIntrinsic(sllInst, n, Const(eSize - shift));

                Instruction srlInst = X86PsrlInstruction[op.Size];

                res = context.AddIntrinsic(srlInst, res, Const(eSize - 1));

                Operand nSrl = context.AddIntrinsic(srlInst, n, Const(shift));

                Instruction addInst = X86PaddInstruction[op.Size];

                res = context.AddIntrinsic(addInst, res, nSrl);
                res = context.AddIntrinsic(addInst, res, d);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorShrImmOpZx(context, ShrImmFlags.Round | ShrImmFlags.Accumulate);
            }
        }

        public static void Ushl_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractZx(context, op.Rn, index, op.Size);
                Operand me = EmitVectorExtractZx(context, op.Rm, index, op.Size);

                MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.UnsignedShlReg));

                Operand e = context.Call(info, ne, me, Const(0), Const(op.Size));

                res = EmitVectorInsert(context, res, e, index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Ushll_V(EmitterContext context)
        {
            OpCodeSimdShImm op = (OpCodeSimdShImm)context.CurrOp;

            int shift = GetImmShl(op);

            if (Optimizations.UseSse41)
            {
                Operand n = GetVec(op.Rn);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Instruction.X86Psrldq, n, Const(8));
                }

                Instruction movzxInst = X86PmovzxInstruction[op.Size];

                Operand res = context.AddIntrinsic(movzxInst, n);

                if (shift != 0)
                {
                    Instruction sllInst = X86PsllInstruction[op.Size + 1];

                    res = context.AddIntrinsic(sllInst, res, Const(shift));
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorShImmWidenBinaryZx(context, (op1, op2) => context.ShiftLeft(op1, op2), shift);
            }
        }

        public static void Ushr_S(EmitterContext context)
        {
            EmitShrImmOp(context, ShrImmFlags.ScalarZx);
        }

        public static void Ushr_V(EmitterContext context)
        {
            OpCodeSimdShImm op = (OpCodeSimdShImm)context.CurrOp;

            if (Optimizations.UseSse2 && op.Size > 0)
            {
                int shift = GetImmShr(op);

                Operand n = GetVec(op.Rn);

                Instruction srlInst = X86PsrlInstruction[op.Size];

                Operand res = context.AddIntrinsic(srlInst, n, Const(shift));

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitShrImmOp(context, ShrImmFlags.VectorZx);
            }
        }

        public static void Usra_S(EmitterContext context)
        {
            EmitScalarShrImmOpZx(context, ShrImmFlags.Accumulate);
        }

        public static void Usra_V(EmitterContext context)
        {
            OpCodeSimdShImm op = (OpCodeSimdShImm)context.CurrOp;

            if (Optimizations.UseSse2 && op.Size > 0)
            {
                int shift = GetImmShr(op);

                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);

                Instruction srlInst = X86PsrlInstruction[op.Size];

                Operand res = context.AddIntrinsic(srlInst, n, Const(shift));

                Instruction addInst = X86PaddInstruction[op.Size];

                res = context.AddIntrinsic(addInst, res, d);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(d, res);
            }
            else
            {
                EmitVectorShrImmOpZx(context, ShrImmFlags.Accumulate);
            }
        }

        [Flags]
        private enum ShrImmFlags
        {
            Scalar = 1 << 0,
            Signed = 1 << 1,

            Round      = 1 << 2,
            Accumulate = 1 << 3,

            ScalarSx = Scalar | Signed,
            ScalarZx = Scalar,

            VectorSx = Signed,
            VectorZx = 0
        }

        private static void EmitScalarShrImmOpSx(EmitterContext context, ShrImmFlags flags)
        {
            EmitShrImmOp(context, ShrImmFlags.ScalarSx | flags);
        }

        private static void EmitScalarShrImmOpZx(EmitterContext context, ShrImmFlags flags)
        {
            EmitShrImmOp(context, ShrImmFlags.ScalarZx | flags);
        }

        private static void EmitVectorShrImmOpSx(EmitterContext context, ShrImmFlags flags)
        {
            EmitShrImmOp(context, ShrImmFlags.VectorSx | flags);
        }

        private static void EmitVectorShrImmOpZx(EmitterContext context, ShrImmFlags flags)
        {
            EmitShrImmOp(context, ShrImmFlags.VectorZx | flags);
        }

        private static void EmitShrImmOp(EmitterContext context, ShrImmFlags flags)
        {
            OpCodeSimdShImm op = (OpCodeSimdShImm)context.CurrOp;

            Operand res = context.VectorZero();

            bool scalar     = (flags & ShrImmFlags.Scalar)     != 0;
            bool signed     = (flags & ShrImmFlags.Signed)     != 0;
            bool round      = (flags & ShrImmFlags.Round)      != 0;
            bool accumulate = (flags & ShrImmFlags.Accumulate) != 0;

            int shift = GetImmShr(op);

            long roundConst = 1L << (shift - 1);

            int elems = !scalar ? op.GetBytesCount() >> op.Size : 1;

            for (int index = 0; index < elems; index++)
            {
                Operand e = EmitVectorExtract(context, op.Rn, index, op.Size, signed);

                if (op.Size <= 2)
                {
                    if (round)
                    {
                        e = context.Add(e, Const(roundConst));
                    }

                    e = signed
                        ? context.ShiftRightSI(e, Const(shift))
                        : context.ShiftRightUI(e, Const(shift));
                }
                else /* if (op.Size == 3) */
                {
                    e = EmitShrImm64(context, e, signed, round ? roundConst : 0L, shift);
                }

                if (accumulate)
                {
                    Operand de = EmitVectorExtract(context, op.Rd, index, op.Size, signed);

                    e = context.Add(e, de);
                }

                res = EmitVectorInsert(context, res, e, index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        private static void EmitVectorShrImmNarrowOpZx(EmitterContext context, bool round)
        {
            OpCodeSimdShImm op = (OpCodeSimdShImm)context.CurrOp;

            int shift = GetImmShr(op);

            long roundConst = 1L << (shift - 1);

            int elems = 8 >> op.Size;

            int part = op.RegisterSize == RegisterSize.Simd128 ? elems : 0;

            Operand res = part == 0 ? context.VectorZero() : context.Copy(GetVec(op.Rd));

            for (int index = 0; index < elems; index++)
            {
                Operand e = EmitVectorExtractZx(context, op.Rn, index, op.Size + 1);

                if (round)
                {
                    e = context.Add(e, Const(roundConst));
                }

                e = context.ShiftRightUI(e, Const(shift));

                res = EmitVectorInsert(context, res, e, part + index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        [Flags]
        private enum ShrImmSaturatingNarrowFlags
        {
            Scalar    = 1 << 0,
            SignedSrc = 1 << 1,
            SignedDst = 1 << 2,

            Round = 1 << 3,

            ScalarSxSx = Scalar | SignedSrc | SignedDst,
            ScalarSxZx = Scalar | SignedSrc,
            ScalarZxZx = Scalar,

            VectorSxSx = SignedSrc | SignedDst,
            VectorSxZx = SignedSrc,
            VectorZxZx = 0
        }

        private static void EmitRoundShrImmSaturatingNarrowOp(EmitterContext context, ShrImmSaturatingNarrowFlags flags)
        {
            EmitShrImmSaturatingNarrowOp(context, ShrImmSaturatingNarrowFlags.Round | flags);
        }

        private static void EmitShrImmSaturatingNarrowOp(EmitterContext context, ShrImmSaturatingNarrowFlags flags)
        {
            OpCodeSimdShImm op = (OpCodeSimdShImm)context.CurrOp;

            bool scalar    = (flags & ShrImmSaturatingNarrowFlags.Scalar)    != 0;
            bool signedSrc = (flags & ShrImmSaturatingNarrowFlags.SignedSrc) != 0;
            bool signedDst = (flags & ShrImmSaturatingNarrowFlags.SignedDst) != 0;
            bool round     = (flags & ShrImmSaturatingNarrowFlags.Round)     != 0;

            int shift = GetImmShr(op);

            long roundConst = 1L << (shift - 1);

            int elems = !scalar ? 8 >> op.Size : 1;

            int part = !scalar && (op.RegisterSize == RegisterSize.Simd128) ? elems : 0;

            Operand res = part == 0 ? context.VectorZero() : context.Copy(GetVec(op.Rd));

            for (int index = 0; index < elems; index++)
            {
                Operand e = EmitVectorExtract(context, op.Rn, index, op.Size + 1, signedSrc);

                if (op.Size <= 1 || !round)
                {
                    if (round)
                    {
                        e = context.Add(e, Const(roundConst));
                    }

                    e = signedSrc
                        ? context.ShiftRightSI(e, Const(shift))
                        : context.ShiftRightUI(e, Const(shift));
                }
                else /* if (op.Size == 2 && round) */
                {
                    e = EmitShrImm64(context, e, signedSrc, roundConst, shift); // shift <= 32
                }

                e = EmitSatQ(context, e, op.Size, signedSrc, signedDst);

                res = EmitVectorInsert(context, res, e, part + index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        // dst64 = (Int(src64, signed) + roundConst) >> shift;
        private static Operand EmitShrImm64(
            EmitterContext context,
            Operand value,
            bool signed,
            long roundConst,
            int shift)
        {
            string name = signed
                ? nameof(SoftFallback.SignedShrImm64)
                : nameof(SoftFallback.UnsignedShrImm64);

            MethodInfo info = typeof(SoftFallback).GetMethod(name);

            return context.Call(info, value, Const(roundConst), Const(shift));
        }

        private static void EmitVectorShImmWidenBinarySx(EmitterContext context, Func2I emit, int imm)
        {
            EmitVectorShImmWidenBinaryOp(context, emit, imm, signed: true);
        }

        private static void EmitVectorShImmWidenBinaryZx(EmitterContext context, Func2I emit, int imm)
        {
            EmitVectorShImmWidenBinaryOp(context, emit, imm, signed: false);
        }

        private static void EmitVectorShImmWidenBinaryOp(EmitterContext context, Func2I emit, int imm, bool signed)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = 8 >> op.Size;

            int part = op.RegisterSize == RegisterSize.Simd128 ? elems : 0;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtract(context, op.Rn, part + index, op.Size, signed);

                res = EmitVectorInsert(context, res, emit(ne, Const(imm)), index, op.Size + 1);
            }

            context.Copy(GetVec(op.Rd), res);
        }
    }
}
