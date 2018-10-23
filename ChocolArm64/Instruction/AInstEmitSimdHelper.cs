using ChocolArm64.Decoder;
using ChocolArm64.State;
using ChocolArm64.Translation;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ChocolArm64.Instruction
{
    static class AInstEmitSimdHelper
    {
        public static readonly Type[] IntTypesPerSizeLog2 = new Type[]
        {
            typeof(sbyte),
            typeof(short),
            typeof(int),
            typeof(long)
        };

        public static readonly Type[] UIntTypesPerSizeLog2 = new Type[]
        {
            typeof(byte),
            typeof(ushort),
            typeof(uint),
            typeof(ulong)
        };

        public static readonly Type[] VectorIntTypesPerSizeLog2 = new Type[]
        {
            typeof(Vector128<sbyte>),
            typeof(Vector128<short>),
            typeof(Vector128<int>),
            typeof(Vector128<long>)
        };

        public static readonly Type[] VectorUIntTypesPerSizeLog2 = new Type[]
        {
            typeof(Vector128<byte>),
            typeof(Vector128<ushort>),
            typeof(Vector128<uint>),
            typeof(Vector128<ulong>)
        };

        [Flags]
        public enum OperFlags
        {
            Rd = 1 << 0,
            Rn = 1 << 1,
            Rm = 1 << 2,
            Ra = 1 << 3,

            RnRm   = Rn | Rm,
            RdRn   = Rd | Rn,
            RaRnRm = Ra | Rn | Rm,
            RdRnRm = Rd | Rn | Rm
        }

        public static int GetImmShl(AOpCodeSimdShImm op)
        {
            return op.Imm - (8 << op.Size);
        }

        public static int GetImmShr(AOpCodeSimdShImm op)
        {
            return (8 << (op.Size + 1)) - op.Imm;
        }

        public static void EmitSse2Op(AilEmitterCtx context, string name)
        {
            EmitSseOp(context, name, typeof(Sse2));
        }

        public static void EmitSse41Op(AilEmitterCtx context, string name)
        {
            EmitSseOp(context, name, typeof(Sse41));
        }

        public static void EmitSse42Op(AilEmitterCtx context, string name)
        {
            EmitSseOp(context, name, typeof(Sse42));
        }

        private static void EmitSseOp(AilEmitterCtx context, string name, Type type)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            EmitLdvecWithSignedCast(context, op.Rn, op.Size);

            Type baseType = VectorIntTypesPerSizeLog2[op.Size];

            if (op is AOpCodeSimdReg binOp)
            {
                EmitLdvecWithSignedCast(context, binOp.Rm, op.Size);

                context.EmitCall(type.GetMethod(name, new Type[] { baseType, baseType }));
            }
            else
            {
                context.EmitCall(type.GetMethod(name, new Type[] { baseType }));
            }

            EmitStvecWithSignedCast(context, op.Rd, op.Size);

            if (op.RegisterSize == ARegisterSize.Simd64)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        public static void EmitLdvecWithSignedCast(AilEmitterCtx context, int reg, int size)
        {
            context.EmitLdvec(reg);

            switch (size)
            {
                case 0: AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorSingleToSByte)); break;
                case 1: AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorSingleToInt16)); break;
                case 2: AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorSingleToInt32)); break;
                case 3: AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorSingleToInt64)); break;

                default: throw new ArgumentOutOfRangeException(nameof(size));
            }
        }

        public static void EmitLdvecWithCastToDouble(AilEmitterCtx context, int reg)
        {
            context.EmitLdvec(reg);

            AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorSingleToDouble));
        }

        public static void EmitStvecWithCastFromDouble(AilEmitterCtx context, int reg)
        {
            AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorDoubleToSingle));

            context.EmitStvec(reg);
        }

        public static void EmitLdvecWithUnsignedCast(AilEmitterCtx context, int reg, int size)
        {
            context.EmitLdvec(reg);

            switch (size)
            {
                case 0: AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorSingleToByte));   break;
                case 1: AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorSingleToUInt16)); break;
                case 2: AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorSingleToUInt32)); break;
                case 3: AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorSingleToUInt64)); break;

                default: throw new ArgumentOutOfRangeException(nameof(size));
            }
        }

        public static void EmitStvecWithSignedCast(AilEmitterCtx context, int reg, int size)
        {
            switch (size)
            {
                case 0: AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorSByteToSingle)); break;
                case 1: AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorInt16ToSingle)); break;
                case 2: AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorInt32ToSingle)); break;
                case 3: AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorInt64ToSingle)); break;

                default: throw new ArgumentOutOfRangeException(nameof(size));
            }

            context.EmitStvec(reg);
        }

        public static void EmitStvecWithUnsignedCast(AilEmitterCtx context, int reg, int size)
        {
            switch (size)
            {
                case 0: AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorByteToSingle));   break;
                case 1: AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorUInt16ToSingle)); break;
                case 2: AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorUInt32ToSingle)); break;
                case 3: AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorUInt64ToSingle)); break;

                default: throw new ArgumentOutOfRangeException(nameof(size));
            }

            context.EmitStvec(reg);
        }

        public static void EmitScalarSseOrSse2OpF(AilEmitterCtx context, string name)
        {
            EmitSseOrSse2OpF(context, name, true);
        }

        public static void EmitVectorSseOrSse2OpF(AilEmitterCtx context, string name)
        {
            EmitSseOrSse2OpF(context, name, false);
        }

        public static void EmitSseOrSse2OpF(AilEmitterCtx context, string name, bool scalar)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            void Ldvec(int reg)
            {
                context.EmitLdvec(reg);

                if (sizeF == 1)
                {
                    AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorSingleToDouble));
                }
            }

            Ldvec(op.Rn);

            Type type;
            Type baseType;

            if (sizeF == 0)
            {
                type     = typeof(Sse);
                baseType = typeof(Vector128<float>);
            }
            else /* if (SizeF == 1) */
            {
                type     = typeof(Sse2);
                baseType = typeof(Vector128<double>);
            }

            if (op is AOpCodeSimdReg binOp)
            {
                Ldvec(binOp.Rm);

                context.EmitCall(type.GetMethod(name, new Type[] { baseType, baseType }));
            }
            else
            {
                context.EmitCall(type.GetMethod(name, new Type[] { baseType }));
            }

            if (sizeF == 1)
            {
                AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorDoubleToSingle));
            }

            context.EmitStvec(op.Rd);

            if (scalar)
            {
                if (sizeF == 0)
                {
                    EmitVectorZero32_128(context, op.Rd);
                }
                else /* if (SizeF == 1) */
                {
                    EmitVectorZeroUpper(context, op.Rd);
                }
            }
            else if (op.RegisterSize == ARegisterSize.Simd64)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        public static void EmitUnaryMathCall(AilEmitterCtx context, string name)
        {
            IAOpCodeSimd op = (IAOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            MethodInfo mthdInfo;

            if (sizeF == 0)
            {
                mthdInfo = typeof(MathF).GetMethod(name, new Type[] { typeof(float) });
            }
            else /* if (SizeF == 1) */
            {
                mthdInfo = typeof(Math).GetMethod(name, new Type[] { typeof(double) });
            }

            context.EmitCall(mthdInfo);
        }

        public static void EmitBinaryMathCall(AilEmitterCtx context, string name)
        {
            IAOpCodeSimd op = (IAOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            MethodInfo mthdInfo;

            if (sizeF == 0)
            {
                mthdInfo = typeof(MathF).GetMethod(name, new Type[] { typeof(float), typeof(float) });
            }
            else /* if (SizeF == 1) */
            {
                mthdInfo = typeof(Math).GetMethod(name, new Type[] { typeof(double), typeof(double) });
            }

            context.EmitCall(mthdInfo);
        }

        public static void EmitRoundMathCall(AilEmitterCtx context, MidpointRounding roundMode)
        {
            IAOpCodeSimd op = (IAOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            MethodInfo mthdInfo;

            if (sizeF == 0)
            {
                mthdInfo = typeof(MathF).GetMethod(nameof(MathF.Round), new Type[] { typeof(float), typeof(MidpointRounding) });
            }
            else /* if (SizeF == 1) */
            {
                mthdInfo = typeof(Math).GetMethod(nameof(Math.Round), new Type[] { typeof(double), typeof(MidpointRounding) });
            }

            context.EmitLdc_I4((int)roundMode);

            context.EmitCall(mthdInfo);
        }

        public static void EmitUnarySoftFloatCall(AilEmitterCtx context, string name)
        {
            IAOpCodeSimd op = (IAOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            MethodInfo mthdInfo;

            if (sizeF == 0)
            {
                mthdInfo = typeof(ASoftFloat).GetMethod(name, new Type[] { typeof(float) });
            }
            else /* if (SizeF == 1) */
            {
                mthdInfo = typeof(ASoftFloat).GetMethod(name, new Type[] { typeof(double) });
            }

            context.EmitCall(mthdInfo);
        }

        public static void EmitSoftFloatCall(AilEmitterCtx context, string name)
        {
            IAOpCodeSimd op = (IAOpCodeSimd)context.CurrOp;

            Type type = (op.Size & 1) == 0
                ? typeof(ASoftFloat32)
                : typeof(ASoftFloat64);

            context.EmitLdarg(ATranslatedSub.StateArgIdx);

            context.EmitCall(type, name);
        }

        public static void EmitScalarBinaryOpByElemF(AilEmitterCtx context, Action emit)
        {
            AOpCodeSimdRegElemF op = (AOpCodeSimdRegElemF)context.CurrOp;

            EmitScalarOpByElemF(context, emit, op.Index, ternary: false);
        }

        public static void EmitScalarTernaryOpByElemF(AilEmitterCtx context, Action emit)
        {
            AOpCodeSimdRegElemF op = (AOpCodeSimdRegElemF)context.CurrOp;

            EmitScalarOpByElemF(context, emit, op.Index, ternary: true);
        }

        public static void EmitScalarOpByElemF(AilEmitterCtx context, Action emit, int elem, bool ternary)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            int sizeF = op.Size & 1;

            if (ternary)
            {
                EmitVectorExtractF(context, op.Rd, 0, sizeF);
            }

            EmitVectorExtractF(context, op.Rn, 0,    sizeF);
            EmitVectorExtractF(context, op.Rm, elem, sizeF);

            emit();

            EmitScalarSetF(context, op.Rd, sizeF);
        }

        public static void EmitScalarUnaryOpSx(AilEmitterCtx context, Action emit)
        {
            EmitScalarOp(context, emit, OperFlags.Rn, true);
        }

        public static void EmitScalarBinaryOpSx(AilEmitterCtx context, Action emit)
        {
            EmitScalarOp(context, emit, OperFlags.RnRm, true);
        }

        public static void EmitScalarUnaryOpZx(AilEmitterCtx context, Action emit)
        {
            EmitScalarOp(context, emit, OperFlags.Rn, false);
        }

        public static void EmitScalarBinaryOpZx(AilEmitterCtx context, Action emit)
        {
            EmitScalarOp(context, emit, OperFlags.RnRm, false);
        }

        public static void EmitScalarTernaryOpZx(AilEmitterCtx context, Action emit)
        {
            EmitScalarOp(context, emit, OperFlags.RdRnRm, false);
        }

        public static void EmitScalarOp(AilEmitterCtx context, Action emit, OperFlags opers, bool signed)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            bool rd = (opers & OperFlags.Rd) != 0;
            bool rn = (opers & OperFlags.Rn) != 0;
            bool rm = (opers & OperFlags.Rm) != 0;

            if (rd)
            {
                EmitVectorExtract(context, op.Rd, 0, op.Size, signed);
            }

            if (rn)
            {
                EmitVectorExtract(context, op.Rn, 0, op.Size, signed);
            }

            if (rm)
            {
                EmitVectorExtract(context, ((AOpCodeSimdReg)op).Rm, 0, op.Size, signed);
            }

            emit();

            EmitScalarSet(context, op.Rd, op.Size);
        }

        public static void EmitScalarUnaryOpF(AilEmitterCtx context, Action emit)
        {
            EmitScalarOpF(context, emit, OperFlags.Rn);
        }

        public static void EmitScalarBinaryOpF(AilEmitterCtx context, Action emit)
        {
            EmitScalarOpF(context, emit, OperFlags.RnRm);
        }

        public static void EmitScalarTernaryRaOpF(AilEmitterCtx context, Action emit)
        {
            EmitScalarOpF(context, emit, OperFlags.RaRnRm);
        }

        public static void EmitScalarOpF(AilEmitterCtx context, Action emit, OperFlags opers)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            bool ra = (opers & OperFlags.Ra) != 0;
            bool rn = (opers & OperFlags.Rn) != 0;
            bool rm = (opers & OperFlags.Rm) != 0;

            if (ra)
            {
                EmitVectorExtractF(context, ((AOpCodeSimdReg)op).Ra, 0, sizeF);
            }

            if (rn)
            {
                EmitVectorExtractF(context, op.Rn, 0, sizeF);
            }

            if (rm)
            {
                EmitVectorExtractF(context, ((AOpCodeSimdReg)op).Rm, 0, sizeF);
            }

            emit();

            EmitScalarSetF(context, op.Rd, sizeF);
        }

        public static void EmitVectorUnaryOpF(AilEmitterCtx context, Action emit)
        {
            EmitVectorOpF(context, emit, OperFlags.Rn);
        }

        public static void EmitVectorBinaryOpF(AilEmitterCtx context, Action emit)
        {
            EmitVectorOpF(context, emit, OperFlags.RnRm);
        }

        public static void EmitVectorTernaryOpF(AilEmitterCtx context, Action emit)
        {
            EmitVectorOpF(context, emit, OperFlags.RdRnRm);
        }

        public static void EmitVectorOpF(AilEmitterCtx context, Action emit, OperFlags opers)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            int bytes = op.GetBitsCount() >> 3;
            int elems = bytes >> sizeF + 2;

            bool rd = (opers & OperFlags.Rd) != 0;
            bool rn = (opers & OperFlags.Rn) != 0;
            bool rm = (opers & OperFlags.Rm) != 0;

            for (int index = 0; index < elems; index++)
            {
                if (rd)
                {
                    EmitVectorExtractF(context, op.Rd, index, sizeF);
                }

                if (rn)
                {
                    EmitVectorExtractF(context, op.Rn, index, sizeF);
                }

                if (rm)
                {
                    EmitVectorExtractF(context, ((AOpCodeSimdReg)op).Rm, index, sizeF);
                }

                emit();

                EmitVectorInsertF(context, op.Rd, index, sizeF);
            }

            if (op.RegisterSize == ARegisterSize.Simd64)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        public static void EmitVectorBinaryOpByElemF(AilEmitterCtx context, Action emit)
        {
            AOpCodeSimdRegElemF op = (AOpCodeSimdRegElemF)context.CurrOp;

            EmitVectorOpByElemF(context, emit, op.Index, ternary: false);
        }

        public static void EmitVectorTernaryOpByElemF(AilEmitterCtx context, Action emit)
        {
            AOpCodeSimdRegElemF op = (AOpCodeSimdRegElemF)context.CurrOp;

            EmitVectorOpByElemF(context, emit, op.Index, ternary: true);
        }

        public static void EmitVectorOpByElemF(AilEmitterCtx context, Action emit, int elem, bool ternary)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            int sizeF = op.Size & 1;

            int bytes = op.GetBitsCount() >> 3;
            int elems = bytes >> sizeF + 2;

            for (int index = 0; index < elems; index++)
            {
                if (ternary)
                {
                    EmitVectorExtractF(context, op.Rd, index, sizeF);
                }

                EmitVectorExtractF(context, op.Rn, index, sizeF);
                EmitVectorExtractF(context, op.Rm, elem,  sizeF);

                emit();

                EmitVectorInsertTmpF(context, index, sizeF);
            }

            context.EmitLdvectmp();
            context.EmitStvec(op.Rd);

            if (op.RegisterSize == ARegisterSize.Simd64)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        public static void EmitVectorUnaryOpSx(AilEmitterCtx context, Action emit)
        {
            EmitVectorOp(context, emit, OperFlags.Rn, true);
        }

        public static void EmitVectorBinaryOpSx(AilEmitterCtx context, Action emit)
        {
            EmitVectorOp(context, emit, OperFlags.RnRm, true);
        }

        public static void EmitVectorTernaryOpSx(AilEmitterCtx context, Action emit)
        {
            EmitVectorOp(context, emit, OperFlags.RdRnRm, true);
        }

        public static void EmitVectorUnaryOpZx(AilEmitterCtx context, Action emit)
        {
            EmitVectorOp(context, emit, OperFlags.Rn, false);
        }

        public static void EmitVectorBinaryOpZx(AilEmitterCtx context, Action emit)
        {
            EmitVectorOp(context, emit, OperFlags.RnRm, false);
        }

        public static void EmitVectorTernaryOpZx(AilEmitterCtx context, Action emit)
        {
            EmitVectorOp(context, emit, OperFlags.RdRnRm, false);
        }

        public static void EmitVectorOp(AilEmitterCtx context, Action emit, OperFlags opers, bool signed)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            int bytes = op.GetBitsCount() >> 3;
            int elems = bytes >> op.Size;

            bool rd = (opers & OperFlags.Rd) != 0;
            bool rn = (opers & OperFlags.Rn) != 0;
            bool rm = (opers & OperFlags.Rm) != 0;

            for (int index = 0; index < elems; index++)
            {
                if (rd)
                {
                    EmitVectorExtract(context, op.Rd, index, op.Size, signed);
                }

                if (rn)
                {
                    EmitVectorExtract(context, op.Rn, index, op.Size, signed);
                }

                if (rm)
                {
                    EmitVectorExtract(context, ((AOpCodeSimdReg)op).Rm, index, op.Size, signed);
                }

                emit();

                EmitVectorInsert(context, op.Rd, index, op.Size);
            }

            if (op.RegisterSize == ARegisterSize.Simd64)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        public static void EmitVectorBinaryOpByElemSx(AilEmitterCtx context, Action emit)
        {
            AOpCodeSimdRegElem op = (AOpCodeSimdRegElem)context.CurrOp;

            EmitVectorOpByElem(context, emit, op.Index, false, true);
        }

        public static void EmitVectorBinaryOpByElemZx(AilEmitterCtx context, Action emit)
        {
            AOpCodeSimdRegElem op = (AOpCodeSimdRegElem)context.CurrOp;

            EmitVectorOpByElem(context, emit, op.Index, false, false);
        }

        public static void EmitVectorTernaryOpByElemZx(AilEmitterCtx context, Action emit)
        {
            AOpCodeSimdRegElem op = (AOpCodeSimdRegElem)context.CurrOp;

            EmitVectorOpByElem(context, emit, op.Index, true, false);
        }

        public static void EmitVectorOpByElem(AilEmitterCtx context, Action emit, int elem, bool ternary, bool signed)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            int bytes = op.GetBitsCount() >> 3;
            int elems = bytes >> op.Size;

            EmitVectorExtract(context, op.Rm, elem, op.Size, signed);
            context.EmitSttmp();

            for (int index = 0; index < elems; index++)
            {
                if (ternary)
                {
                    EmitVectorExtract(context, op.Rd, index, op.Size, signed);
                }

                EmitVectorExtract(context, op.Rn, index, op.Size, signed);
                context.EmitLdtmp();

                emit();

                EmitVectorInsertTmp(context, index, op.Size);
            }

            context.EmitLdvectmp();
            context.EmitStvec(op.Rd);

            if (op.RegisterSize == ARegisterSize.Simd64)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        public static void EmitVectorImmUnaryOp(AilEmitterCtx context, Action emit)
        {
            EmitVectorImmOp(context, emit, false);
        }

        public static void EmitVectorImmBinaryOp(AilEmitterCtx context, Action emit)
        {
            EmitVectorImmOp(context, emit, true);
        }

        public static void EmitVectorImmOp(AilEmitterCtx context, Action emit, bool binary)
        {
            AOpCodeSimdImm op = (AOpCodeSimdImm)context.CurrOp;

            int bytes = op.GetBitsCount() >> 3;
            int elems = bytes >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                if (binary)
                {
                    EmitVectorExtractZx(context, op.Rd, index, op.Size);
                }

                context.EmitLdc_I8(op.Imm);

                emit();

                EmitVectorInsert(context, op.Rd, index, op.Size);
            }

            if (op.RegisterSize == ARegisterSize.Simd64)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        public static void EmitVectorWidenRmBinaryOpSx(AilEmitterCtx context, Action emit)
        {
            EmitVectorWidenRmBinaryOp(context, emit, true);
        }

        public static void EmitVectorWidenRmBinaryOpZx(AilEmitterCtx context, Action emit)
        {
            EmitVectorWidenRmBinaryOp(context, emit, false);
        }

        public static void EmitVectorWidenRmBinaryOp(AilEmitterCtx context, Action emit, bool signed)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            int elems = 8 >> op.Size;

            int part = op.RegisterSize == ARegisterSize.Simd128 ? elems : 0;

            for (int index = 0; index < elems; index++)
            {
                EmitVectorExtract(context, op.Rn,        index, op.Size + 1, signed);
                EmitVectorExtract(context, op.Rm, part + index, op.Size,     signed);

                emit();

                EmitVectorInsertTmp(context, index, op.Size + 1);
            }

            context.EmitLdvectmp();
            context.EmitStvec(op.Rd);
        }

        public static void EmitVectorWidenRnRmBinaryOpSx(AilEmitterCtx context, Action emit)
        {
            EmitVectorWidenRnRmOp(context, emit, false, true);
        }

        public static void EmitVectorWidenRnRmBinaryOpZx(AilEmitterCtx context, Action emit)
        {
            EmitVectorWidenRnRmOp(context, emit, false, false);
        }

        public static void EmitVectorWidenRnRmTernaryOpSx(AilEmitterCtx context, Action emit)
        {
            EmitVectorWidenRnRmOp(context, emit, true, true);
        }

        public static void EmitVectorWidenRnRmTernaryOpZx(AilEmitterCtx context, Action emit)
        {
            EmitVectorWidenRnRmOp(context, emit, true, false);
        }

        public static void EmitVectorWidenRnRmOp(AilEmitterCtx context, Action emit, bool ternary, bool signed)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            int elems = 8 >> op.Size;

            int part = op.RegisterSize == ARegisterSize.Simd128 ? elems : 0;

            for (int index = 0; index < elems; index++)
            {
                if (ternary)
                {
                    EmitVectorExtract(context, op.Rd, index, op.Size + 1, signed);
                }

                EmitVectorExtract(context, op.Rn, part + index, op.Size, signed);
                EmitVectorExtract(context, op.Rm, part + index, op.Size, signed);

                emit();

                EmitVectorInsertTmp(context, index, op.Size + 1);
            }

            context.EmitLdvectmp();
            context.EmitStvec(op.Rd);
        }

        public static void EmitVectorPairwiseOpSx(AilEmitterCtx context, Action emit)
        {
            EmitVectorPairwiseOp(context, emit, true);
        }

        public static void EmitVectorPairwiseOpZx(AilEmitterCtx context, Action emit)
        {
            EmitVectorPairwiseOp(context, emit, false);
        }

        public static void EmitVectorPairwiseOp(AilEmitterCtx context, Action emit, bool signed)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            int words = op.GetBitsCount() >> 4;
            int pairs = words >> op.Size;

            for (int index = 0; index < pairs; index++)
            {
                int idx = index << 1;

                EmitVectorExtract(context, op.Rn, idx,     op.Size, signed);
                EmitVectorExtract(context, op.Rn, idx + 1, op.Size, signed);

                emit();

                EmitVectorExtract(context, op.Rm, idx,     op.Size, signed);
                EmitVectorExtract(context, op.Rm, idx + 1, op.Size, signed);

                emit();

                EmitVectorInsertTmp(context, pairs + index, op.Size);
                EmitVectorInsertTmp(context,         index, op.Size);
            }

            context.EmitLdvectmp();
            context.EmitStvec(op.Rd);

            if (op.RegisterSize == ARegisterSize.Simd64)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        public static void EmitVectorPairwiseOpF(AilEmitterCtx context, Action emit)
        {
            AOpCodeSimdReg op = (AOpCodeSimdReg)context.CurrOp;

            int sizeF = op.Size & 1;

            int words = op.GetBitsCount() >> 4;
            int pairs = words >> sizeF + 2;

            for (int index = 0; index < pairs; index++)
            {
                int idx = index << 1;

                EmitVectorExtractF(context, op.Rn, idx,     sizeF);
                EmitVectorExtractF(context, op.Rn, idx + 1, sizeF);

                emit();

                EmitVectorExtractF(context, op.Rm, idx,     sizeF);
                EmitVectorExtractF(context, op.Rm, idx + 1, sizeF);

                emit();

                EmitVectorInsertTmpF(context, pairs + index, sizeF);
                EmitVectorInsertTmpF(context,         index, sizeF);
            }

            context.EmitLdvectmp();
            context.EmitStvec(op.Rd);

            if (op.RegisterSize == ARegisterSize.Simd64)
            {
                EmitVectorZeroUpper(context, op.Rd);
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

        public static void EmitScalarSaturatingUnaryOpSx(AilEmitterCtx context, Action emit)
        {
            EmitSaturatingUnaryOpSx(context, emit, SaturatingFlags.ScalarSx);
        }

        public static void EmitVectorSaturatingUnaryOpSx(AilEmitterCtx context, Action emit)
        {
            EmitSaturatingUnaryOpSx(context, emit, SaturatingFlags.VectorSx);
        }

        public static void EmitSaturatingUnaryOpSx(AilEmitterCtx context, Action emit, SaturatingFlags flags)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            bool scalar = (flags & SaturatingFlags.Scalar) != 0;

            int bytes = op.GetBitsCount() >> 3;
            int elems = !scalar ? bytes >> op.Size : 1;

            if (scalar)
            {
                EmitVectorZeroLowerTmp(context);
            }

            for (int index = 0; index < elems; index++)
            {
                EmitVectorExtractSx(context, op.Rn, index, op.Size);

                emit();

                if (op.Size <= 2)
                {
                    EmitSatQ(context, op.Size, true, true);
                }
                else /* if (Op.Size == 3) */
                {
                    EmitUnarySignedSatQAbsOrNeg(context);
                }

                EmitVectorInsertTmp(context, index, op.Size);
            }

            context.EmitLdvectmp();
            context.EmitStvec(op.Rd);

            if ((op.RegisterSize == ARegisterSize.Simd64) || scalar)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        public static void EmitScalarSaturatingBinaryOpSx(AilEmitterCtx context, SaturatingFlags flags)
        {
            EmitSaturatingBinaryOp(context, () => { }, SaturatingFlags.ScalarSx | flags);
        }

        public static void EmitScalarSaturatingBinaryOpZx(AilEmitterCtx context, SaturatingFlags flags)
        {
            EmitSaturatingBinaryOp(context, () => { }, SaturatingFlags.ScalarZx | flags);
        }

        public static void EmitVectorSaturatingBinaryOpSx(AilEmitterCtx context, SaturatingFlags flags)
        {
            EmitSaturatingBinaryOp(context, () => { }, SaturatingFlags.VectorSx | flags);
        }

        public static void EmitVectorSaturatingBinaryOpZx(AilEmitterCtx context, SaturatingFlags flags)
        {
            EmitSaturatingBinaryOp(context, () => { }, SaturatingFlags.VectorZx | flags);
        }

        public static void EmitSaturatingBinaryOp(AilEmitterCtx context, Action emit, SaturatingFlags flags)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            bool scalar = (flags & SaturatingFlags.Scalar) != 0;
            bool signed = (flags & SaturatingFlags.Signed) != 0;

            bool add = (flags & SaturatingFlags.Add) != 0;
            bool sub = (flags & SaturatingFlags.Sub) != 0;

            bool accumulate = (flags & SaturatingFlags.Accumulate) != 0;

            int bytes = op.GetBitsCount() >> 3;
            int elems = !scalar ? bytes >> op.Size : 1;

            if (scalar)
            {
                EmitVectorZeroLowerTmp(context);
            }

            if (add || sub)
            {
                for (int index = 0; index < elems; index++)
                {
                    EmitVectorExtract(context,                   op.Rn, index, op.Size, signed);
                    EmitVectorExtract(context, ((AOpCodeSimdReg)op).Rm, index, op.Size, signed);

                    if (op.Size <= 2)
                    {
                        context.Emit(add ? OpCodes.Add : OpCodes.Sub);

                        EmitSatQ(context, op.Size, true, signed);
                    }
                    else /* if (Op.Size == 3) */
                    {
                        if (add)
                        {
                            EmitBinarySatQAdd(context, signed);
                        }
                        else /* if (Sub) */
                        {
                            EmitBinarySatQSub(context, signed);
                        }
                    }

                    EmitVectorInsertTmp(context, index, op.Size);
                }
            }
            else if (accumulate)
            {
                for (int index = 0; index < elems; index++)
                {
                    EmitVectorExtract(context, op.Rn, index, op.Size, !signed);
                    EmitVectorExtract(context, op.Rd, index, op.Size,  signed);

                    if (op.Size <= 2)
                    {
                        context.Emit(OpCodes.Add);

                        EmitSatQ(context, op.Size, true, signed);
                    }
                    else /* if (Op.Size == 3) */
                    {
                        EmitBinarySatQAccumulate(context, signed);
                    }

                    EmitVectorInsertTmp(context, index, op.Size);
                }
            }
            else
            {
                for (int index = 0; index < elems; index++)
                {
                    EmitVectorExtract(context,                   op.Rn, index, op.Size, signed);
                    EmitVectorExtract(context, ((AOpCodeSimdReg)op).Rm, index, op.Size, signed);

                    emit();

                    EmitSatQ(context, op.Size, true, signed);

                    EmitVectorInsertTmp(context, index, op.Size);
                }
            }

            context.EmitLdvectmp();
            context.EmitStvec(op.Rd);

            if ((op.RegisterSize == ARegisterSize.Simd64) || scalar)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
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

        public static void EmitSaturatingNarrowOp(AilEmitterCtx context, SaturatingNarrowFlags flags)
        {
            AOpCodeSimd op = (AOpCodeSimd)context.CurrOp;

            bool scalar    = (flags & SaturatingNarrowFlags.Scalar)    != 0;
            bool signedSrc = (flags & SaturatingNarrowFlags.SignedSrc) != 0;
            bool signedDst = (flags & SaturatingNarrowFlags.SignedDst) != 0;

            int elems = !scalar ? 8 >> op.Size : 1;

            int part = !scalar && (op.RegisterSize == ARegisterSize.Simd128) ? elems : 0;

            if (scalar)
            {
                EmitVectorZeroLowerTmp(context);
            }

            if (part != 0)
            {
                context.EmitLdvec(op.Rd);
                context.EmitStvectmp();
            }

            for (int index = 0; index < elems; index++)
            {
                EmitVectorExtract(context, op.Rn, index, op.Size + 1, signedSrc);

                EmitSatQ(context, op.Size, signedSrc, signedDst);

                EmitVectorInsertTmp(context, part + index, op.Size);
            }

            context.EmitLdvectmp();
            context.EmitStvec(op.Rd);

            if (part == 0)
            {
                EmitVectorZeroUpper(context, op.Rd);
            }
        }

        // TSrc (16bit, 32bit, 64bit; signed, unsigned) > TDst (8bit, 16bit, 32bit; signed, unsigned).
        public static void EmitSatQ(
            AilEmitterCtx context,
            int  sizeDst,
            bool signedSrc,
            bool signedDst)
        {
            if (sizeDst > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeDst));
            }

            context.EmitLdc_I4(sizeDst);
            context.EmitLdarg(ATranslatedSub.StateArgIdx);

            if (signedSrc)
            {
                ASoftFallback.EmitCall(context, signedDst
                    ? nameof(ASoftFallback.SignedSrcSignedDstSatQ)
                    : nameof(ASoftFallback.SignedSrcUnsignedDstSatQ));
            }
            else
            {
                ASoftFallback.EmitCall(context, signedDst
                    ? nameof(ASoftFallback.UnsignedSrcSignedDstSatQ)
                    : nameof(ASoftFallback.UnsignedSrcUnsignedDstSatQ));
            }
        }

        // TSrc (64bit) == TDst (64bit); signed.
        public static void EmitUnarySignedSatQAbsOrNeg(AilEmitterCtx context)
        {
            if (((AOpCodeSimd)context.CurrOp).Size < 3)
            {
                throw new InvalidOperationException();
            }

            context.EmitLdarg(ATranslatedSub.StateArgIdx);

            ASoftFallback.EmitCall(context, nameof(ASoftFallback.UnarySignedSatQAbsOrNeg));
        }

        // TSrcs (64bit) == TDst (64bit); signed, unsigned.
        public static void EmitBinarySatQAdd(AilEmitterCtx context, bool signed)
        {
            if (((AOpCodeSimdReg)context.CurrOp).Size < 3)
            {
                throw new InvalidOperationException();
            }

            context.EmitLdarg(ATranslatedSub.StateArgIdx);

            ASoftFallback.EmitCall(context, signed
                ? nameof(ASoftFallback.BinarySignedSatQAdd)
                : nameof(ASoftFallback.BinaryUnsignedSatQAdd));
        }

        // TSrcs (64bit) == TDst (64bit); signed, unsigned.
        public static void EmitBinarySatQSub(AilEmitterCtx context, bool signed)
        {
            if (((AOpCodeSimdReg)context.CurrOp).Size < 3)
            {
                throw new InvalidOperationException();
            }

            context.EmitLdarg(ATranslatedSub.StateArgIdx);

            ASoftFallback.EmitCall(context, signed
                ? nameof(ASoftFallback.BinarySignedSatQSub)
                : nameof(ASoftFallback.BinaryUnsignedSatQSub));
        }

        // TSrcs (64bit) == TDst (64bit); signed, unsigned.
        public static void EmitBinarySatQAccumulate(AilEmitterCtx context, bool signed)
        {
            if (((AOpCodeSimd)context.CurrOp).Size < 3)
            {
                throw new InvalidOperationException();
            }

            context.EmitLdarg(ATranslatedSub.StateArgIdx);

            ASoftFallback.EmitCall(context, signed
                ? nameof(ASoftFallback.BinarySignedSatQAcc)
                : nameof(ASoftFallback.BinaryUnsignedSatQAcc));
        }

        public static void EmitScalarSet(AilEmitterCtx context, int reg, int size)
        {
            EmitVectorZeroAll(context, reg);
            EmitVectorInsert(context, reg, 0, size);
        }

        public static void EmitScalarSetF(AilEmitterCtx context, int reg, int size)
        {
            if (AOptimizations.UseSse41 && size == 0)
            {
                //If the type is float, we can perform insertion and
                //zero the upper bits with a single instruction (INSERTPS);
                context.EmitLdvec(reg);

                AVectorHelper.EmitCall(context, nameof(AVectorHelper.Sse41VectorInsertScalarSingle));

                context.EmitStvec(reg);
            }
            else
            {
                EmitVectorZeroAll(context, reg);
                EmitVectorInsertF(context, reg, 0, size);
            }
        }

        public static void EmitVectorExtractSx(AilEmitterCtx context, int reg, int index, int size)
        {
            EmitVectorExtract(context, reg, index, size, true);
        }

        public static void EmitVectorExtractZx(AilEmitterCtx context, int reg, int index, int size)
        {
            EmitVectorExtract(context, reg, index, size, false);
        }

        public static void EmitVectorExtract(AilEmitterCtx context, int reg, int index, int size, bool signed)
        {
            ThrowIfInvalid(index, size);

            context.EmitLdvec(reg);
            context.EmitLdc_I4(index);
            context.EmitLdc_I4(size);

            AVectorHelper.EmitCall(context, signed
                ? nameof(AVectorHelper.VectorExtractIntSx)
                : nameof(AVectorHelper.VectorExtractIntZx));
        }

        public static void EmitVectorExtractF(AilEmitterCtx context, int reg, int index, int size)
        {
            ThrowIfInvalidF(index, size);

            context.EmitLdvec(reg);
            context.EmitLdc_I4(index);

            if (size == 0)
            {
                AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorExtractSingle));
            }
            else if (size == 1)
            {
                AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorExtractDouble));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }
        }

        public static void EmitVectorZeroAll(AilEmitterCtx context, int rd)
        {
            if (AOptimizations.UseSse2)
            {
                AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorSingleZero));

                context.EmitStvec(rd);
            }
            else
            {
                EmitVectorZeroLower(context, rd);
                EmitVectorZeroUpper(context, rd);
            }
        }

        public static void EmitVectorZeroLower(AilEmitterCtx context, int rd)
        {
            EmitVectorInsert(context, rd, 0, 3, 0);
        }

        public static void EmitVectorZeroLowerTmp(AilEmitterCtx context)
        {
            EmitVectorInsertTmp(context, 0, 3, 0);
        }

        public static void EmitVectorZeroUpper(AilEmitterCtx context, int reg)
        {
            if (AOptimizations.UseSse2)
            {
                //TODO: Use MoveScalar once it is fixed, as of the
                //time of writing it just crashes the JIT.
                EmitLdvecWithUnsignedCast(context, reg, 3);

                Type[] types = new Type[] { typeof(Vector128<ulong>), typeof(byte) };

                //Context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.MoveScalar), Types));

                context.EmitLdc_I4(8);

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftLeftLogical128BitLane), types));

                context.EmitLdc_I4(8);

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical128BitLane), types));

                EmitStvecWithUnsignedCast(context, reg, 3);
            }
            else
            {
                EmitVectorInsert(context, reg, 1, 3, 0);
            }
        }

        public static void EmitVectorZero32_128(AilEmitterCtx context, int reg)
        {
            context.EmitLdvec(reg);

            AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorZero32_128));

            context.EmitStvec(reg);
        }

        public static void EmitVectorInsert(AilEmitterCtx context, int reg, int index, int size)
        {
            ThrowIfInvalid(index, size);

            context.EmitLdvec(reg);
            context.EmitLdc_I4(index);
            context.EmitLdc_I4(size);

            AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorInsertInt));

            context.EmitStvec(reg);
        }

        public static void EmitVectorInsertTmp(AilEmitterCtx context, int index, int size)
        {
            ThrowIfInvalid(index, size);

            context.EmitLdvectmp();
            context.EmitLdc_I4(index);
            context.EmitLdc_I4(size);

            AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorInsertInt));

            context.EmitStvectmp();
        }

        public static void EmitVectorInsert(AilEmitterCtx context, int reg, int index, int size, long value)
        {
            ThrowIfInvalid(index, size);

            context.EmitLdc_I8(value);
            context.EmitLdvec(reg);
            context.EmitLdc_I4(index);
            context.EmitLdc_I4(size);

            AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorInsertInt));

            context.EmitStvec(reg);
        }

        public static void EmitVectorInsertTmp(AilEmitterCtx context, int index, int size, long value)
        {
            ThrowIfInvalid(index, size);

            context.EmitLdc_I8(value);
            context.EmitLdvectmp();
            context.EmitLdc_I4(index);
            context.EmitLdc_I4(size);

            AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorInsertInt));

            context.EmitStvectmp();
        }

        public static void EmitVectorInsertF(AilEmitterCtx context, int reg, int index, int size)
        {
            ThrowIfInvalidF(index, size);

            context.EmitLdvec(reg);
            context.EmitLdc_I4(index);

            if (size == 0)
            {
                AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorInsertSingle));
            }
            else if (size == 1)
            {
                AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorInsertDouble));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            context.EmitStvec(reg);
        }

        public static void EmitVectorInsertTmpF(AilEmitterCtx context, int index, int size)
        {
            ThrowIfInvalidF(index, size);

            context.EmitLdvectmp();
            context.EmitLdc_I4(index);

            if (size == 0)
            {
                AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorInsertSingle));
            }
            else if (size == 1)
            {
                AVectorHelper.EmitCall(context, nameof(AVectorHelper.VectorInsertDouble));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            context.EmitStvectmp();
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

        private static void ThrowIfInvalidF(int index, int size)
        {
            if ((uint)size > 1u)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            if ((uint)index >= 4u >> size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
