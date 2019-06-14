using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System;
using System.Diagnostics;
using System.Reflection;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    using Func1I = Func<Operand, Operand>;
    using Func2I = Func<Operand, Operand, Operand>;
    using Func3I = Func<Operand, Operand, Operand, Operand>;

    static class InstEmitSimdHelper
    {
#region "X86 SSE Instructions"
        public static readonly Instruction[] X86PaddInstruction = new Instruction[]
        {
            Instruction.X86Paddb,
            Instruction.X86Paddw,
            Instruction.X86Paddd,
            Instruction.X86Paddq
        };

        public static readonly Instruction[] X86PcmpeqInstruction = new Instruction[]
        {
            Instruction.X86Pcmpeqb,
            Instruction.X86Pcmpeqw,
            Instruction.X86Pcmpeqd,
            Instruction.X86Pcmpeqq
        };

        public static readonly Instruction[] X86PcmpgtInstruction = new Instruction[]
        {
            Instruction.X86Pcmpgtb,
            Instruction.X86Pcmpgtw,
            Instruction.X86Pcmpgtd,
            Instruction.X86Pcmpgtq
        };

        public static readonly Instruction[] X86PmaxsInstruction = new Instruction[]
        {
            Instruction.X86Pmaxsb,
            Instruction.X86Pmaxsw,
            Instruction.X86Pmaxsd
        };

        public static readonly Instruction[] X86PmaxuInstruction = new Instruction[]
        {
            Instruction.X86Pmaxub,
            Instruction.X86Pmaxuw,
            Instruction.X86Pmaxud
        };

        public static readonly Instruction[] X86PminsInstruction = new Instruction[]
        {
            Instruction.X86Pminsb,
            Instruction.X86Pminsw,
            Instruction.X86Pminsd
        };

        public static readonly Instruction[] X86PminuInstruction = new Instruction[]
        {
            Instruction.X86Pminub,
            Instruction.X86Pminuw,
            Instruction.X86Pminud
        };

        public static readonly Instruction[] X86PmovsxInstruction = new Instruction[]
        {
            Instruction.X86Pmovsxbw,
            Instruction.X86Pmovsxwd,
            Instruction.X86Pmovsxdq
        };

        public static readonly Instruction[] X86PmovzxInstruction = new Instruction[]
        {
            Instruction.X86Pmovzxbw,
            Instruction.X86Pmovzxwd,
            Instruction.X86Pmovzxdq
        };

        public static readonly Instruction[] X86PsubInstruction = new Instruction[]
        {
            Instruction.X86Psubb,
            Instruction.X86Psubw,
            Instruction.X86Psubd,
            Instruction.X86Psubq
        };

        public static readonly Instruction[] X86PunpckhInstruction = new Instruction[]
        {
            Instruction.X86Punpckhbw,
            Instruction.X86Punpckhwd,
            Instruction.X86Punpckhdq,
            Instruction.X86Punpckhqdq
        };

        public static readonly Instruction[] X86PunpcklInstruction = new Instruction[]
        {
            Instruction.X86Punpcklbw,
            Instruction.X86Punpcklwd,
            Instruction.X86Punpckldq,
            Instruction.X86Punpcklqdq
        };
#endregion

        public static int GetImmShl(OpCodeSimdShImm op)
        {
            return op.Imm - (8 << op.Size);
        }

        public static int GetImmShr(OpCodeSimdShImm op)
        {
            return (8 << (op.Size + 1)) - op.Imm;
        }

        public static Operand X86GetScalar(EmitterContext context, float value)
        {
            return X86GetScalar(context, BitConverter.SingleToInt32Bits(value));
        }

        public static Operand X86GetScalar(EmitterContext context, double value)
        {
            return X86GetScalar(context, BitConverter.DoubleToInt64Bits(value));
        }

        public static Operand X86GetScalar(EmitterContext context, int value)
        {
            return context.Copy(Local(OperandType.V128), Const(value));
        }

        public static Operand X86GetScalar(EmitterContext context, long value)
        {
            return context.Copy(Local(OperandType.V128), Const(value));
        }

        public static Operand X86GetAllElements(EmitterContext context, float value)
        {
            return X86GetAllElements(context, BitConverter.SingleToInt32Bits(value));
        }

        public static Operand X86GetAllElements(EmitterContext context, double value)
        {
            return X86GetAllElements(context, BitConverter.DoubleToInt64Bits(value));
        }

        public static Operand X86GetAllElements(EmitterContext context, int value)
        {
            Operand vector = context.Copy(Local(OperandType.V128), Const(value));

            vector = context.AddIntrinsic(Instruction.X86Shufps, vector, vector, Const(0));

            return vector;
        }

        public static Operand X86GetAllElements(EmitterContext context, long value)
        {
            Operand vector = context.Copy(Local(OperandType.V128), Const(value));

            vector = context.AddIntrinsic(Instruction.X86Movlhps, vector, vector);

            return vector;
        }

        public static int X86GetRoundControl(FPRoundingMode roundMode)
        {
            switch (roundMode)
            {
                case FPRoundingMode.ToNearest:            return 8 | 0;
                case FPRoundingMode.TowardsPlusInfinity:  return 8 | 2;
                case FPRoundingMode.TowardsMinusInfinity: return 8 | 1;
                case FPRoundingMode.TowardsZero:          return 8 | 3;
            }

            throw new ArgumentException($"Invalid rounding mode \"{roundMode}\".");
        }

        public static void EmitScalarUnaryOpF(
            EmitterContext context,
            Instruction inst32,
            Instruction inst64)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand n = GetVec(op.Rn);

            Instruction inst = (op.Size & 1) != 0 ? inst64 : inst32;

            Operand res = context.AddIntrinsic(inst, n);

            if ((op.Size & 1) != 0)
            {
                res = context.VectorZeroUpper64(res);
            }
            else
            {
                res = context.VectorZeroUpper96(res);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitScalarBinaryOpF(
            EmitterContext context,
            Instruction inst32,
            Instruction inst64)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand n = GetVec(op.Rn);
            Operand m = GetVec(op.Rm);

            Instruction inst = (op.Size & 1) != 0 ? inst64 : inst32;

            Operand res = context.AddIntrinsic(inst, n, m);

            if ((op.Size & 1) != 0)
            {
                res = context.VectorZeroUpper64(res);
            }
            else
            {
                res = context.VectorZeroUpper96(res);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorUnaryOpF(
            EmitterContext context,
            Instruction inst32,
            Instruction inst64)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand n = GetVec(op.Rn);

            Instruction inst = (op.Size & 1) != 0 ? inst64 : inst32;

            Operand res = context.AddIntrinsic(inst, n);

            if (op.RegisterSize == RegisterSize.Simd64)
            {
                res = context.VectorZeroUpper64(res);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorBinaryOpF(
            EmitterContext context,
            Instruction inst32,
            Instruction inst64)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand n = GetVec(op.Rn);
            Operand m = GetVec(op.Rm);

            Instruction inst = (op.Size & 1) != 0 ? inst64 : inst32;

            Operand res = context.AddIntrinsic(inst, n, m);

            if (op.RegisterSize == RegisterSize.Simd64)
            {
                res = context.VectorZeroUpper64(res);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static Operand EmitUnaryMathCall(EmitterContext context, string name, Operand n)
        {
            IOpCodeSimd op = (IOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            MethodInfo info;

            if (sizeF == 0)
            {
                info = typeof(MathF).GetMethod(name, new Type[] { typeof(float) });
            }
            else /* if (sizeF == 1) */
            {
                info = typeof(Math).GetMethod(name, new Type[] { typeof(double) });
            }

            return context.Call(info, n);
        }

        public static Operand EmitBinaryMathCall(EmitterContext context, string name, Operand n)
        {
            IOpCodeSimd op = (IOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            MethodInfo info;

            if (sizeF == 0)
            {
                info = typeof(MathF).GetMethod(name, new Type[] { typeof(float), typeof(float) });
            }
            else /* if (sizeF == 1) */
            {
                info = typeof(Math).GetMethod(name, new Type[] { typeof(double), typeof(double) });
            }

            return context.Call(info, n);
        }

        public static Operand EmitRoundMathCall(EmitterContext context, MidpointRounding roundMode, Operand n)
        {
            IOpCodeSimd op = (IOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            MethodInfo info;

            if (sizeF == 0)
            {
                info = typeof(MathF).GetMethod(nameof(MathF.Round), new Type[] { typeof(float), typeof(MidpointRounding) });
            }
            else /* if (sizeF == 1) */
            {
                info = typeof(Math).GetMethod(nameof(Math.Round), new Type[] { typeof(double), typeof(MidpointRounding) });
            }

            return context.Call(info, n, Const((int)roundMode));
        }

        public static Operand EmitSoftFloatCall(EmitterContext context, string name, params Operand[] callArgs)
        {
            IOpCodeSimd op = (IOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            Type type = sizeF == 0 ? typeof(SoftFloat32)
                                   : typeof(SoftFloat64);

            return context.Call(type.GetMethod(name), callArgs);
        }

        public static void EmitScalarBinaryOpByElemF(EmitterContext context, Func2I emit)
        {
            OpCodeSimdRegElemF op = (OpCodeSimdRegElemF)context.CurrOp;

            OperandType type = (op.Size & 1) != 0 ? OperandType.FP64 : OperandType.FP32;

            Operand n = context.VectorExtract(GetVec(op.Rn), Local(type), 0);
            Operand m = context.VectorExtract(GetVec(op.Rm), Local(type), op.Index);

            context.Copy(GetVec(op.Rd), context.VectorInsert(context.VectorZero(), emit(n, m), 0));
        }

        public static void EmitScalarTernaryOpByElemF(EmitterContext context, Func3I emit)
        {
            OpCodeSimdRegElemF op = (OpCodeSimdRegElemF)context.CurrOp;

            OperandType type = (op.Size & 1) != 0 ? OperandType.FP64 : OperandType.FP32;

            Operand d = context.VectorExtract(GetVec(op.Rd), Local(type), 0);
            Operand n = context.VectorExtract(GetVec(op.Rn), Local(type), 0);
            Operand m = context.VectorExtract(GetVec(op.Rm), Local(type), op.Index);

            context.Copy(GetVec(op.Rd), context.VectorInsert(context.VectorZero(), emit(d, n, m), 0));
        }

        public static void EmitScalarUnaryOpSx(EmitterContext context, Func1I emit)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand n = EmitVectorExtractSx(context, op.Rn, 0, op.Size);

            Operand d = EmitVectorInsert(context, context.VectorZero(), emit(n), 0, op.Size);

            context.Copy(GetVec(op.Rd), d);
        }

        public static void EmitScalarBinaryOpSx(EmitterContext context, Func2I emit)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand n = EmitVectorExtractSx(context, op.Rn, 0, op.Size);
            Operand m = EmitVectorExtractSx(context, op.Rm, 0, op.Size);

            Operand d = EmitVectorInsert(context, context.VectorZero(), emit(n, m), 0, op.Size);

            context.Copy(GetVec(op.Rd), d);
        }

        public static void EmitScalarUnaryOpZx(EmitterContext context, Func1I emit)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand n = EmitVectorExtractZx(context, op.Rn, 0, op.Size);

            Operand d = EmitVectorInsert(context, context.VectorZero(), emit(n), 0, op.Size);

            context.Copy(GetVec(op.Rd), d);
        }

        public static void EmitScalarBinaryOpZx(EmitterContext context, Func2I emit)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand n = EmitVectorExtractZx(context, op.Rn, 0, op.Size);
            Operand m = EmitVectorExtractZx(context, op.Rm, 0, op.Size);

            Operand d = EmitVectorInsert(context, context.VectorZero(), emit(n, m), 0, op.Size);

            context.Copy(GetVec(op.Rd), d);
        }

        public static void EmitScalarTernaryOpZx(EmitterContext context, Func3I emit)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand d = EmitVectorExtractZx(context, op.Rd, 0, op.Size);
            Operand n = EmitVectorExtractZx(context, op.Rn, 0, op.Size);
            Operand m = EmitVectorExtractZx(context, op.Rm, 0, op.Size);

            d = EmitVectorInsert(context, context.VectorZero(), emit(d, n, m), 0, op.Size);

            context.Copy(GetVec(op.Rd), d);
        }

        public static void EmitScalarUnaryOpF(EmitterContext context, Func1I emit)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            OperandType type = (op.Size & 1) != 0 ? OperandType.FP64 : OperandType.FP32;

            Operand n = context.VectorExtract(GetVec(op.Rn), Local(type), 0);

            context.Copy(GetVec(op.Rd), context.VectorInsert(context.VectorZero(), emit(n), 0));
        }

        public static void EmitScalarBinaryOpF(EmitterContext context, Func2I emit)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            OperandType type = (op.Size & 1) != 0 ? OperandType.FP64 : OperandType.FP32;

            Operand n = context.VectorExtract(GetVec(op.Rn), Local(type), 0);
            Operand m = context.VectorExtract(GetVec(op.Rm), Local(type), 0);

            context.Copy(GetVec(op.Rd), context.VectorInsert(context.VectorZero(), emit(n, m), 0));
        }

        public static void EmitScalarTernaryRaOpF(EmitterContext context, Func3I emit)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            OperandType type = (op.Size & 1) != 0 ? OperandType.FP64 : OperandType.FP32;

            Operand a = context.VectorExtract(GetVec(op.Ra), Local(type), 0);
            Operand n = context.VectorExtract(GetVec(op.Rn), Local(type), 0);
            Operand m = context.VectorExtract(GetVec(op.Rm), Local(type), 0);

            context.Copy(GetVec(op.Rd), context.VectorInsert(context.VectorZero(), emit(a, n, m), 0));
        }

        public static void EmitVectorUnaryOpF(EmitterContext context, Func1I emit)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand res = context.VectorZero();

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;

            int elems = op.GetBytesCount() >> sizeF + 2;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = context.VectorExtract(GetVec(op.Rn), Local(type), index);

                res = context.VectorInsert(res, emit(ne), index);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorBinaryOpF(EmitterContext context, Func2I emit)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;

            int elems = op.GetBytesCount() >> sizeF + 2;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = context.VectorExtract(GetVec(op.Rn), Local(type), index);
                Operand me = context.VectorExtract(GetVec(op.Rm), Local(type), index);

                res = context.VectorInsert(res, emit(ne, me), index);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorTernaryOpF(EmitterContext context, Func3I emit)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;

            int elems = op.GetBytesCount() >> sizeF + 2;

            for (int index = 0; index < elems; index++)
            {
                Operand de = context.VectorExtract(GetVec(op.Rd), Local(type), index);
                Operand ne = context.VectorExtract(GetVec(op.Rn), Local(type), index);
                Operand me = context.VectorExtract(GetVec(op.Rm), Local(type), index);

                res = context.VectorInsert(res, emit(de, ne, me), index);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorBinaryOpByElemF(EmitterContext context, Func2I emit)
        {
            OpCodeSimdRegElemF op = (OpCodeSimdRegElemF)context.CurrOp;

            Operand res = context.VectorZero();

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;

            int elems = op.GetBytesCount() >> sizeF + 2;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = context.VectorExtract(GetVec(op.Rn), Local(type), index);
                Operand me = context.VectorExtract(GetVec(op.Rm), Local(type), op.Index);

                res = context.VectorInsert(res, emit(ne, me), index);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorTernaryOpByElemF(EmitterContext context, Func3I emit)
        {
            OpCodeSimdRegElemF op = (OpCodeSimdRegElemF)context.CurrOp;

            Operand res = context.VectorZero();

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;

            int elems = op.GetBytesCount() >> sizeF + 2;

            for (int index = 0; index < elems; index++)
            {
                Operand de = context.VectorExtract(GetVec(op.Rd), Local(type), index);
                Operand ne = context.VectorExtract(GetVec(op.Rn), Local(type), index);
                Operand me = context.VectorExtract(GetVec(op.Rm), Local(type), op.Index);

                res = context.VectorInsert(res, emit(de, ne, me), index);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorUnaryOpSx(EmitterContext context, Func1I emit)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractSx(context, op.Rn, index, op.Size);

                res = EmitVectorInsert(context, res, emit(ne), index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorBinaryOpSx(EmitterContext context, Func2I emit)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractSx(context, op.Rn, index, op.Size);
                Operand me = EmitVectorExtractSx(context, op.Rm, index, op.Size);

                res = EmitVectorInsert(context, res, emit(ne, me), index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorTernaryOpSx(EmitterContext context, Func3I emit)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand de = EmitVectorExtractSx(context, op.Rd, index, op.Size);
                Operand ne = EmitVectorExtractSx(context, op.Rn, index, op.Size);
                Operand me = EmitVectorExtractSx(context, op.Rm, index, op.Size);

                res = EmitVectorInsert(context, res, emit(de, ne, me), index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorUnaryOpZx(EmitterContext context, Func1I emit)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractZx(context, op.Rn, index, op.Size);

                res = EmitVectorInsert(context, res, emit(ne), index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorBinaryOpZx(EmitterContext context, Func2I emit)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractZx(context, op.Rn, index, op.Size);
                Operand me = EmitVectorExtractZx(context, op.Rm, index, op.Size);

                res = EmitVectorInsert(context, res, emit(ne, me), index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorTernaryOpZx(EmitterContext context, Func3I emit)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand de = EmitVectorExtractZx(context, op.Rd, index, op.Size);
                Operand ne = EmitVectorExtractZx(context, op.Rn, index, op.Size);
                Operand me = EmitVectorExtractZx(context, op.Rm, index, op.Size);

                res = EmitVectorInsert(context, res, emit(de, ne, me), index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorBinaryOpByElemSx(EmitterContext context, Func2I emit)
        {
            OpCodeSimdRegElem op = (OpCodeSimdRegElem)context.CurrOp;

            Operand res = context.VectorZero();

            Operand me = EmitVectorExtractSx(context, op.Rm, op.Index, op.Size);

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractSx(context, op.Rn, index, op.Size);

                res = EmitVectorInsert(context, res, emit(ne, me), index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorBinaryOpByElemZx(EmitterContext context, Func2I emit)
        {
            OpCodeSimdRegElem op = (OpCodeSimdRegElem)context.CurrOp;

            Operand res = context.VectorZero();

            Operand me = EmitVectorExtractZx(context, op.Rm, op.Index, op.Size);

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractZx(context, op.Rn, index, op.Size);

                res = EmitVectorInsert(context, res, emit(ne, me), index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorTernaryOpByElemZx(EmitterContext context, Func3I emit)
        {
            OpCodeSimdRegElem op = (OpCodeSimdRegElem)context.CurrOp;

            Operand res = context.VectorZero();

            Operand me = EmitVectorExtractZx(context, op.Rm, op.Index, op.Size);

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand de = EmitVectorExtractZx(context, op.Rd, index, op.Size);
                Operand ne = EmitVectorExtractZx(context, op.Rn, index, op.Size);

                res = EmitVectorInsert(context, res, emit(de, ne, me), index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorImmUnaryOp(EmitterContext context, Func1I emit)
        {
            OpCodeSimdImm op = (OpCodeSimdImm)context.CurrOp;

            Operand imm = Const(op.Immediate);

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                res = EmitVectorInsert(context, res, emit(imm), index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorImmBinaryOp(EmitterContext context, Func2I emit)
        {
            OpCodeSimdImm op = (OpCodeSimdImm)context.CurrOp;

            Operand imm = Const(op.Immediate);

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand de = EmitVectorExtractZx(context, op.Rd, index, op.Size);

                res = EmitVectorInsert(context, res, emit(de, imm), index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorWidenRmBinaryOpSx(EmitterContext context, Func2I emit)
        {
            EmitVectorWidenRmBinaryOp(context, emit, signed: true);
        }

        public static void EmitVectorWidenRmBinaryOpZx(EmitterContext context, Func2I emit)
        {
            EmitVectorWidenRmBinaryOp(context, emit, signed: false);
        }

        private static void EmitVectorWidenRmBinaryOp(EmitterContext context, Func2I emit, bool signed)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = 8 >> op.Size;

            int part = op.RegisterSize == RegisterSize.Simd128 ? elems : 0;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtract(context, op.Rn,        index, op.Size + 1, signed);
                Operand me = EmitVectorExtract(context, op.Rm, part + index, op.Size,     signed);

                res = EmitVectorInsert(context, res, emit(ne, me), index, op.Size + 1);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorWidenRnRmBinaryOpSx(EmitterContext context, Func2I emit)
        {
            EmitVectorWidenRnRmBinaryOp(context, emit, signed: true);
        }

        public static void EmitVectorWidenRnRmBinaryOpZx(EmitterContext context, Func2I emit)
        {
            EmitVectorWidenRnRmBinaryOp(context, emit, signed: false);
        }

        private static void EmitVectorWidenRnRmBinaryOp(EmitterContext context, Func2I emit, bool signed)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = 8 >> op.Size;

            int part = op.RegisterSize == RegisterSize.Simd128 ? elems : 0;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtract(context, op.Rn, part + index, op.Size, signed);
                Operand me = EmitVectorExtract(context, op.Rm, part + index, op.Size, signed);

                res = EmitVectorInsert(context, res, emit(ne, me), index, op.Size + 1);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorWidenRnRmTernaryOpSx(EmitterContext context, Func3I emit)
        {
            EmitVectorWidenRnRmTernaryOp(context, emit, signed: true);
        }

        public static void EmitVectorWidenRnRmTernaryOpZx(EmitterContext context, Func3I emit)
        {
            EmitVectorWidenRnRmTernaryOp(context, emit, signed: false);
        }

        private static void EmitVectorWidenRnRmTernaryOp(EmitterContext context, Func3I emit, bool signed)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = 8 >> op.Size;

            int part = op.RegisterSize == RegisterSize.Simd128 ? elems : 0;

            for (int index = 0; index < elems; index++)
            {
                Operand de = EmitVectorExtract(context, op.Rd,        index, op.Size + 1, signed);
                Operand ne = EmitVectorExtract(context, op.Rn, part + index, op.Size,     signed);
                Operand me = EmitVectorExtract(context, op.Rm, part + index, op.Size,     signed);

                res = EmitVectorInsert(context, res, emit(de, ne, me), index, op.Size + 1);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorWidenBinaryOpByElemSx(EmitterContext context, Func2I emit)
        {
            EmitVectorWidenBinaryOpByElem(context, emit, signed: true);
        }

        public static void EmitVectorWidenBinaryOpByElemZx(EmitterContext context, Func2I emit)
        {
            EmitVectorWidenBinaryOpByElem(context, emit, signed: false);
        }

        private static void EmitVectorWidenBinaryOpByElem(EmitterContext context, Func2I emit, bool signed)
        {
            OpCodeSimdRegElem op = (OpCodeSimdRegElem)context.CurrOp;

            Operand res = context.VectorZero();

            Operand me = EmitVectorExtract(context, op.Rm, op.Index, op.Size, signed);;

            int elems = 8 >> op.Size;

            int part = op.RegisterSize == RegisterSize.Simd128 ? elems : 0;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtract(context, op.Rn, part + index, op.Size, signed);

                res = EmitVectorInsert(context, res, emit(ne, me), index, op.Size + 1);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorWidenTernaryOpByElemSx(EmitterContext context, Func3I emit)
        {
            EmitVectorWidenTernaryOpByElem(context, emit, signed: true);
        }

        public static void EmitVectorWidenTernaryOpByElemZx(EmitterContext context, Func3I emit)
        {
            EmitVectorWidenTernaryOpByElem(context, emit, signed: false);
        }

        private static void EmitVectorWidenTernaryOpByElem(EmitterContext context, Func3I emit, bool signed)
        {
            OpCodeSimdRegElem op = (OpCodeSimdRegElem)context.CurrOp;

            Operand res = context.VectorZero();

            Operand me = EmitVectorExtract(context, op.Rm, op.Index, op.Size, signed);;

            int elems = 8 >> op.Size;

            int part = op.RegisterSize == RegisterSize.Simd128 ? elems : 0;

            for (int index = 0; index < elems; index++)
            {
                Operand de = EmitVectorExtract(context, op.Rd,        index, op.Size + 1, signed);
                Operand ne = EmitVectorExtract(context, op.Rn, part + index, op.Size,     signed);

                res = EmitVectorInsert(context, res, emit(de, ne, me), index, op.Size + 1);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorPairwiseOpSx(EmitterContext context, Func2I emit)
        {
            EmitVectorPairwiseOp(context, emit, signed: true);
        }

        public static void EmitVectorPairwiseOpZx(EmitterContext context, Func2I emit)
        {
            EmitVectorPairwiseOp(context, emit, signed: false);
        }

        private static void EmitVectorPairwiseOp(EmitterContext context, Func2I emit, bool signed)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int pairs = op.GetPairsCount() >> op.Size;

            for (int index = 0; index < pairs; index++)
            {
                int pairIndex = index << 1;

                Operand n0 = EmitVectorExtract(context, op.Rn, pairIndex,     op.Size, signed);
                Operand n1 = EmitVectorExtract(context, op.Rn, pairIndex + 1, op.Size, signed);

                Operand m0 = EmitVectorExtract(context, op.Rm, pairIndex,     op.Size, signed);
                Operand m1 = EmitVectorExtract(context, op.Rm, pairIndex + 1, op.Size, signed);

                res = EmitVectorInsert(context, res, emit(n0, n1),         index, op.Size);
                res = EmitVectorInsert(context, res, emit(m0, m1), pairs + index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorAcrossVectorOpSx(EmitterContext context, Func2I emit)
        {
            EmitVectorAcrossVectorOp(context, emit, signed: true, isLong: false);
        }

        public static void EmitVectorAcrossVectorOpZx(EmitterContext context, Func2I emit)
        {
            EmitVectorAcrossVectorOp(context, emit, signed: false, isLong: false);
        }

        public static void EmitVectorLongAcrossVectorOpSx(EmitterContext context, Func2I emit)
        {
            EmitVectorAcrossVectorOp(context, emit, signed: true, isLong: true);
        }

        public static void EmitVectorLongAcrossVectorOpZx(EmitterContext context, Func2I emit)
        {
            EmitVectorAcrossVectorOp(context, emit, signed: false, isLong: true);
        }

        private static void EmitVectorAcrossVectorOp(
            EmitterContext context,
            Func2I emit,
            bool signed,
            bool isLong)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            int elems = op.GetBytesCount() >> op.Size;

            Operand res = EmitVectorExtract(context, op.Rn, 0, op.Size, signed);

            for (int index = 1; index < elems; index++)
            {
                Operand n = EmitVectorExtract(context, op.Rn, index, op.Size, signed);

                res = emit(res, n);
            }

            int size = isLong ? op.Size + 1 : op.Size;

            Operand d = EmitVectorInsert(context, context.VectorZero(), res, 0, size);

            context.Copy(GetVec(op.Rd), d);
        }

        public static void EmitVectorPairwiseOpF(EmitterContext context, Func2I emit)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;

            int pairs = op.GetPairsCount() >> sizeF + 2;

            for (int index = 0; index < pairs; index++)
            {
                int pairIndex = index << 1;

                Operand n0 = context.VectorExtract(GetVec(op.Rn), Local(type), pairIndex);
                Operand n1 = context.VectorExtract(GetVec(op.Rn), Local(type), pairIndex + 1);

                Operand m0 = context.VectorExtract(GetVec(op.Rm), Local(type), pairIndex);
                Operand m1 = context.VectorExtract(GetVec(op.Rm), Local(type), pairIndex + 1);

                res = context.VectorInsert(res, emit(n0, n1),         index);
                res = context.VectorInsert(res, emit(m0, m1), pairs + index);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitVectorPairwiseOpF(EmitterContext context, Instruction inst32, Instruction inst64)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand n = GetVec(op.Rn);
            Operand m = GetVec(op.Rm);

            int sizeF = op.Size & 1;

            if (sizeF == 0)
            {
                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    Operand unpck = context.AddIntrinsic(Instruction.X86Unpcklps, n, m);

                    Operand zero = context.VectorZero();

                    Operand part0 = context.AddIntrinsic(Instruction.X86Movlhps, unpck, zero);
                    Operand part1 = context.AddIntrinsic(Instruction.X86Movhlps, zero, unpck);

                    context.Copy(GetVec(op.Rd), context.AddIntrinsic(inst32, part0, part1));
                }
                else /* if (op.RegisterSize == RegisterSize.Simd128) */
                {
                    const int sm0 = 2 << 6 | 0 << 4 | 2 << 2 | 0 << 0;
                    const int sm1 = 3 << 6 | 1 << 4 | 3 << 2 | 1 << 0;

                    Operand part0 = context.AddIntrinsic(Instruction.X86Shufps, n, m, Const(sm0));
                    Operand part1 = context.AddIntrinsic(Instruction.X86Shufps, n, m, Const(sm1));

                    context.Copy(GetVec(op.Rd), context.AddIntrinsic(inst32, part0, part1));
                }
            }
            else /* if (sizeF == 1) */
            {
                Operand part0 = context.AddIntrinsic(Instruction.X86Unpcklpd, n, m);
                Operand part1 = context.AddIntrinsic(Instruction.X86Unpckhpd, n, m);

                context.Copy(GetVec(op.Rd), context.AddIntrinsic(inst64, part0, part1));
            }
        }


        [Flags]
        public enum SaturatingFlags
        {
            Scalar = 1 << 0,
            Signed = 1 << 1,

            Add = 1 << 2,
            Sub = 1 << 3,

            Accumulate = 1 << 4,

            ScalarSx = Scalar | Signed,
            ScalarZx = Scalar,

            VectorSx = Signed,
            VectorZx = 0
        }

        public static void EmitScalarSaturatingUnaryOpSx(EmitterContext context, Func1I emit)
        {
            EmitSaturatingUnaryOpSx(context, emit, SaturatingFlags.ScalarSx);
        }

        public static void EmitVectorSaturatingUnaryOpSx(EmitterContext context, Func1I emit)
        {
            EmitSaturatingUnaryOpSx(context, emit, SaturatingFlags.VectorSx);
        }

        private static void EmitSaturatingUnaryOpSx(EmitterContext context, Func1I emit, SaturatingFlags flags)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand res = context.VectorZero();

            bool scalar = (flags & SaturatingFlags.Scalar) != 0;

            int elems = !scalar ? op.GetBytesCount() >> op.Size : 1;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractSx(context, op.Rn, index, op.Size);
                Operand de;

                if (op.Size <= 2)
                {
                    de = EmitSatQ(context, emit(ne), op.Size, signedSrc: true, signedDst: true);
                }
                else /* if (op.Size == 3) */
                {
                    de = EmitUnarySignedSatQAbsOrNeg(context, emit(ne));
                }

                res = EmitVectorInsert(context, res, de, index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        public static void EmitScalarSaturatingBinaryOpSx(EmitterContext context, SaturatingFlags flags)
        {
            EmitSaturatingBinaryOp(context, null, SaturatingFlags.ScalarSx | flags);
        }

        public static void EmitScalarSaturatingBinaryOpZx(EmitterContext context, SaturatingFlags flags)
        {
            EmitSaturatingBinaryOp(context, null, SaturatingFlags.ScalarZx | flags);
        }

        public static void EmitVectorSaturatingBinaryOpSx(EmitterContext context, SaturatingFlags flags)
        {
            EmitSaturatingBinaryOp(context, null, SaturatingFlags.VectorSx | flags);
        }

        public static void EmitVectorSaturatingBinaryOpZx(EmitterContext context, SaturatingFlags flags)
        {
            EmitSaturatingBinaryOp(context, null, SaturatingFlags.VectorZx | flags);
        }

        public static void EmitSaturatingBinaryOp(EmitterContext context, Func2I emit, SaturatingFlags flags)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand res = context.VectorZero();

            bool scalar = (flags & SaturatingFlags.Scalar) != 0;
            bool signed = (flags & SaturatingFlags.Signed) != 0;

            bool add = (flags & SaturatingFlags.Add) != 0;
            bool sub = (flags & SaturatingFlags.Sub) != 0;

            bool accumulate = (flags & SaturatingFlags.Accumulate) != 0;

            int elems = !scalar ? op.GetBytesCount() >> op.Size : 1;

            if (add || sub)
            {
                OpCodeSimdReg opReg = (OpCodeSimdReg)op;

                for (int index = 0; index < elems; index++)
                {
                    Operand de;
                    Operand ne = EmitVectorExtract(context, opReg.Rn, index, op.Size, signed);
                    Operand me = EmitVectorExtract(context, opReg.Rm, index, op.Size, signed);

                    if (op.Size <= 2)
                    {
                        Operand temp = add ? context.Add     (ne, me)
                                           : context.Subtract(ne, me);

                        de = EmitSatQ(context, temp, op.Size, signedSrc: true, signedDst: signed);
                    }
                    else if (add) /* if (op.Size == 3) */
                    {
                        de = EmitBinarySatQAdd(context, ne, me, signed);
                    }
                    else /* if (sub) */
                    {
                        de = EmitBinarySatQSub(context, ne, me, signed);
                    }

                    res = EmitVectorInsert(context, res, de, index, op.Size);
                }
            }
            else if (accumulate)
            {
                for (int index = 0; index < elems; index++)
                {
                    Operand de;
                    Operand ne = EmitVectorExtract(context, op.Rn, index, op.Size, !signed);
                    Operand me = EmitVectorExtract(context, op.Rd, index, op.Size,  signed);

                    if (op.Size <= 2)
                    {
                        Operand temp = context.Add(ne, me);

                        de = EmitSatQ(context, temp, op.Size, signedSrc: true, signedDst: signed);
                    }
                    else /* if (op.Size == 3) */
                    {
                        de = EmitBinarySatQAccumulate(context, ne, me, signed);
                    }

                    res = EmitVectorInsert(context, res, de, index, op.Size);
                }
            }
            else
            {
                OpCodeSimdReg opReg = (OpCodeSimdReg)op;

                for (int index = 0; index < elems; index++)
                {
                    Operand ne = EmitVectorExtract(context, opReg.Rn, index, op.Size, signed);
                    Operand me = EmitVectorExtract(context, opReg.Rm, index, op.Size, signed);

                    Operand de = EmitSatQ(context, emit(ne, me), op.Size, true, signed);

                    res = EmitVectorInsert(context, res, de, index, op.Size);
                }
            }

            context.Copy(GetVec(op.Rd), res);
        }

        [Flags]
        public enum SaturatingNarrowFlags
        {
            Scalar    = 1 << 0,
            SignedSrc = 1 << 1,
            SignedDst = 1 << 2,

            ScalarSxSx = Scalar | SignedSrc | SignedDst,
            ScalarSxZx = Scalar | SignedSrc,
            ScalarZxZx = Scalar,

            VectorSxSx = SignedSrc | SignedDst,
            VectorSxZx = SignedSrc,
            VectorZxZx = 0
        }

        public static void EmitSaturatingNarrowOp(EmitterContext context, SaturatingNarrowFlags flags)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            bool scalar    = (flags & SaturatingNarrowFlags.Scalar)    != 0;
            bool signedSrc = (flags & SaturatingNarrowFlags.SignedSrc) != 0;
            bool signedDst = (flags & SaturatingNarrowFlags.SignedDst) != 0;

            int elems = !scalar ? 8 >> op.Size : 1;

            int part = !scalar && (op.RegisterSize == RegisterSize.Simd128) ? elems : 0;

            Operand res = part == 0 ? context.VectorZero() : context.Copy(GetVec(op.Rd));

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtract(context, op.Rn, index, op.Size + 1, signedSrc);

                Operand temp = EmitSatQ(context, ne, op.Size, signedSrc, signedDst);

                res = EmitVectorInsert(context, res, temp, part + index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        // TSrc (16bit, 32bit, 64bit; signed, unsigned) > TDst (8bit, 16bit, 32bit; signed, unsigned).
        public static Operand EmitSatQ(EmitterContext context, Operand op, int sizeDst, bool signedSrc, bool signedDst)
        {
            if ((uint)sizeDst > 2u)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeDst));
            }

            string name;

            if (signedSrc)
            {
                name = signedDst ? nameof(SoftFallback.SignedSrcSignedDstSatQ)
                                 : nameof(SoftFallback.SignedSrcUnsignedDstSatQ);
            }
            else
            {
                name = signedDst ? nameof(SoftFallback.UnsignedSrcSignedDstSatQ)
                                 : nameof(SoftFallback.UnsignedSrcUnsignedDstSatQ);
            }

            MethodInfo info = typeof(SoftFallback).GetMethod(name);

            return context.Call(info, op, Const(sizeDst));
        }

        // TSrc (64bit) == TDst (64bit); signed.
        public static Operand EmitUnarySignedSatQAbsOrNeg(EmitterContext context, Operand op)
        {
            Debug.Assert(((OpCodeSimd)context.CurrOp).Size == 3, "Invalid element size.");

            return context.Call(typeof(SoftFallback).GetMethod(nameof(SoftFallback.UnarySignedSatQAbsOrNeg)), op);
        }

        // TSrcs (64bit) == TDst (64bit); signed, unsigned.
        public static Operand EmitBinarySatQAdd(EmitterContext context, Operand op1, Operand op2, bool signed)
        {
            Debug.Assert(((OpCodeSimd)context.CurrOp).Size == 3, "Invalid element size.");

            string name = signed ? nameof(SoftFallback.BinarySignedSatQAdd)
                                 : nameof(SoftFallback.BinaryUnsignedSatQAdd);

            return context.Call(typeof(SoftFallback).GetMethod(name), op1, op2);
        }

        // TSrcs (64bit) == TDst (64bit); signed, unsigned.
        public static Operand EmitBinarySatQSub(EmitterContext context, Operand op1, Operand op2, bool signed)
        {
            Debug.Assert(((OpCodeSimd)context.CurrOp).Size == 3, "Invalid element size.");

            string name = signed ? nameof(SoftFallback.BinarySignedSatQSub)
                                 : nameof(SoftFallback.BinaryUnsignedSatQSub);

            return context.Call(typeof(SoftFallback).GetMethod(name), op1, op2);
        }

        // TSrcs (64bit) == TDst (64bit); signed, unsigned.
        public static Operand EmitBinarySatQAccumulate(EmitterContext context, Operand op1, Operand op2, bool signed)
        {
            Debug.Assert(((OpCodeSimd)context.CurrOp).Size == 3, "Invalid element size.");

            string name = signed ? nameof(SoftFallback.BinarySignedSatQAcc)
                                 : nameof(SoftFallback.BinaryUnsignedSatQAcc);

            return context.Call(typeof(SoftFallback).GetMethod(name), op1, op2);
        }

        public static Operand EmitVectorExtractSx32(EmitterContext context, int reg, int index, int size)
        {
            ThrowIfInvalid(index, size);

            Operand res = Local(OperandType.I32);

            switch (size)
            {
                case 0: context.VectorExtract8 (GetVec(reg), res, index); break;
                case 1: context.VectorExtract16(GetVec(reg), res, index); break;
                case 2: context.VectorExtract  (GetVec(reg), res, index); break;
                case 3: context.VectorExtract  (GetVec(reg), res, index); break;
            }

            switch (size)
            {
                case 0: res = context.SignExtend8 (res); break;
                case 1: res = context.SignExtend16(res); break;
                case 2: res = context.SignExtend32(res); break;
            }

            return res;
        }

        public static Operand EmitVectorExtractSx(EmitterContext context, int reg, int index, int size)
        {
            return EmitVectorExtract(context, reg, index, size, true);
        }

        public static Operand EmitVectorExtractZx(EmitterContext context, int reg, int index, int size)
        {
            return EmitVectorExtract(context, reg, index, size, false);
        }

        public static Operand EmitVectorExtract(EmitterContext context, int reg, int index, int size, bool signed)
        {
            ThrowIfInvalid(index, size);

            Operand res = Local(size == 3 ? OperandType.I64
                                          : OperandType.I32);

            switch (size)
            {
                case 0: context.VectorExtract8 (GetVec(reg), res, index); break;
                case 1: context.VectorExtract16(GetVec(reg), res, index); break;
                case 2: context.VectorExtract  (GetVec(reg), res, index); break;
                case 3: context.VectorExtract  (GetVec(reg), res, index); break;
            }

            res = context.Copy(Local(OperandType.I64), res);

            if (signed)
            {
                switch (size)
                {
                    case 0: res = context.SignExtend8 (res); break;
                    case 1: res = context.SignExtend16(res); break;
                    case 2: res = context.SignExtend32(res); break;
                }
            }

            return res;
        }

        public static Operand EmitVectorInsert(EmitterContext context, Operand vector, Operand value, int index, int size)
        {
            ThrowIfInvalid(index, size);

            if (size < 3)
            {
                value = context.Copy(Local(OperandType.I32), value);
            }

            switch (size)
            {
                case 0: vector = context.VectorInsert8 (vector, value, index); break;
                case 1: vector = context.VectorInsert16(vector, value, index); break;
                case 2: vector = context.VectorInsert  (vector, value, index); break;
                case 3: vector = context.VectorInsert  (vector, value, index); break;
            }

            return vector;
        }

        private static void ThrowIfInvalid(int index, int size)
        {
            if ((uint)size > 3u)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            if ((uint)index >= 16u >> size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
