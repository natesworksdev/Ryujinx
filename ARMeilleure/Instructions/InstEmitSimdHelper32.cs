using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Collections.Generic;
using System.Text;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    using Func1I = Func<Operand, Operand>;
    using Func2I = Func<Operand, Operand, Operand>;
    using Func3I = Func<Operand, Operand, Operand, Operand>;

    static class InstEmitSimdHelper32
    {
        public static (int, int) GetQuadwordAndSubindex(int index, RegisterSize size)
        {
            switch (size)
            {
                case RegisterSize.Simd128:
                    return (index >> 1, 0);
                case RegisterSize.Simd64:
                    return (index >> 1, index & 1);
                case RegisterSize.Simd32:
                    return (index >> 2, index & 3);
            }

            throw new NotImplementedException("Unrecognized Vector Register Size!");
        }

        public static Operand ExtractScalar(ArmEmitterContext context, OperandType type, int reg)
        {
            if (type == OperandType.FP64 || type == OperandType.I64)
            {
                // from dreg
                return context.VectorExtract(type, GetVecA32(reg >> 1), reg & 1);
            } 
            else
            {
                // from sreg
                return context.VectorExtract(type, GetVecA32(reg >> 2), reg & 3);
            }
        }

        public static void InsertScalar(ArmEmitterContext context, int reg, Operand value)
        {
            Operand vec, insert;
            if (value.Type == OperandType.FP64 || value.Type == OperandType.I64)
            {
                // from dreg
                vec = GetVecA32(reg >> 1);
                insert = context.VectorInsert(vec, value, reg & 1);
                
            }
            else
            {
                // from sreg
                vec = GetVecA32(reg >> 2);
                insert = context.VectorInsert(vec, value, reg & 3);
            }
            context.Copy(vec, insert);
        }

        public static void EmitVectorImmUnaryOp32(ArmEmitterContext context, Func1I emit)
        {
            IOpCode32SimdImm op = (IOpCode32SimdImm)context.CurrOp;

            Operand imm = Const(op.Immediate);

            int elems = op.Elems;
            (int index, int subIndex) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

            Operand vec = GetVecA32(index);
            Operand res = vec;

            for (int item = 0; item < elems; item++)
            {
                res = EmitVectorInsert(context, res, emit(imm), item + subIndex * elems, op.Size);
            }

            context.Copy(vec, res);
        }

        public static void EmitScalarUnaryOpF32(ArmEmitterContext context, Func1I emit)
        {
            OpCode32SimdS op = (OpCode32SimdS)context.CurrOp;

            OperandType type = (op.Size & 1) != 0 ? OperandType.FP64 : OperandType.FP32;

            Operand m = ExtractScalar(context, type, op.Vm);

            InsertScalar(context, op.Vd, emit(m));
        }

        public static void EmitScalarBinaryOpF32(ArmEmitterContext context, Func2I emit)
        {
            OpCode32SimdRegS op = (OpCode32SimdRegS)context.CurrOp;

            OperandType type = (op.Size & 1) != 0 ? OperandType.FP64 : OperandType.FP32;

            Operand n = ExtractScalar(context, type, op.Vn);
            Operand m = ExtractScalar(context, type, op.Vm);

            InsertScalar(context, op.Vd, emit(n, m));
        }

        public static void EmitScalarBinaryOpI32(ArmEmitterContext context, Func2I emit)
        {
            OpCode32SimdRegS op = (OpCode32SimdRegS)context.CurrOp;

            OperandType type = (op.Size & 1) != 0 ? OperandType.I64 : OperandType.I32;

            if (op.Size < 2) throw new Exception("Not supported right now");

            Operand n = ExtractScalar(context, type, op.Vn);
            Operand m = ExtractScalar(context, type, op.Vm);

            InsertScalar(context, op.Vd, emit(n, m));
        }

        public static void EmitScalarTernaryOpF32(ArmEmitterContext context, Func3I emit)
        {
            OpCode32SimdRegS op = (OpCode32SimdRegS)context.CurrOp;

            OperandType type = (op.Size & 1) != 0 ? OperandType.FP64 : OperandType.FP32;

            Operand a = ExtractScalar(context, type, op.Vd);
            Operand n = ExtractScalar(context, type, op.Vn);
            Operand m = ExtractScalar(context, type, op.Vm);

            InsertScalar(context, op.Vd, emit(a, n, m));
        }

        public static void EmitVectorUnaryOpF32(ArmEmitterContext context, Func1I emit)
        {
            OpCode32Simd op = (OpCode32Simd)context.CurrOp;

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;

            int elems = op.GetBytesCount() >> sizeF + 2;

            (int vm, int em) = GetQuadwordAndSubindex(op.Vm, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

            Operand res = GetVecA32(vd);

            for (int index = 0; index < elems; index++)
            {
                Operand ne = context.VectorExtract(type, GetVecA32(vm), index + em * elems);

                res = context.VectorInsert(res, emit(ne), index + ed * elems);
            }

            context.Copy(GetVecA32(vd), res);
        }

        public static void EmitVectorBinaryOpF32(ArmEmitterContext context, Func2I emit)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;

            int elems = op.GetBytesCount() >> (sizeF + 2);

            (int vn, int en) = GetQuadwordAndSubindex(op.Vn, op.RegisterSize);
            (int vm, int em) = GetQuadwordAndSubindex(op.Vm, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

            Operand res = GetVecA32(vd);

            for (int index = 0; index < elems; index++)
            {
                Operand ne = context.VectorExtract(type, GetVecA32(vn), index + en * elems);
                Operand me = context.VectorExtract(type, GetVecA32(vm), index + em * elems);

                res = context.VectorInsert(res, emit(ne, me), index + ed * elems);
            }

            context.Copy(GetVecA32(vd), res);
        }

        public static void EmitVectorTernaryOpF32(ArmEmitterContext context, Func3I emit)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;

            int elems = op.GetBytesCount() >> sizeF + 2;

            (int vn, int en) = GetQuadwordAndSubindex(op.Vn, op.RegisterSize);
            (int vm, int em) = GetQuadwordAndSubindex(op.Vm, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

            Operand res = GetVecA32(vd);

            for (int index = 0; index < elems; index++)
            {
                Operand de = context.VectorExtract(type, GetVecA32(vd), index + ed * elems);
                Operand ne = context.VectorExtract(type, GetVecA32(vn), index + en * elems);
                Operand me = context.VectorExtract(type, GetVecA32(vm), index + em * elems);

                res = context.VectorInsert(res, emit(de, ne, me), index);
            }

            context.Copy(GetVecA32(vd), res);
        }

        // INTEGER

        public static void EmitVectorUnaryOpSx32(ArmEmitterContext context, Func1I emit)
        {
            OpCode32Simd op = (OpCode32Simd)context.CurrOp;

            (int vm, int em) = GetQuadwordAndSubindex(op.Vm, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

            Operand res = GetVecA32(vd);

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand me = EmitVectorExtractSx32(context, vm, index + em * elems, op.Size);

                res = EmitVectorInsert(context, res, emit(me), index + ed * elems, op.Size);
            }

            context.Copy(GetVecA32(vd), res);
        }

        public static void EmitVectorBinaryOpSx32(ArmEmitterContext context, Func2I emit)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;

            (int vm, int em) = GetQuadwordAndSubindex(op.Vm, op.RegisterSize);
            (int vn, int en) = GetQuadwordAndSubindex(op.Vn, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

            Operand res = GetVecA32(vd);

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractSx32(context, vn, index + en * elems, op.Size);
                Operand me = EmitVectorExtractSx32(context, vm, index + em * elems, op.Size);

                res = EmitVectorInsert(context, res, emit(ne, me), index + ed * elems, op.Size);
            }

            context.Copy(GetVecA32(vd), res);
        }

        public static void EmitVectorTernaryOpSx32(ArmEmitterContext context, Func3I emit)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;

            (int vm, int em) = GetQuadwordAndSubindex(op.Vm, op.RegisterSize);
            (int vn, int en) = GetQuadwordAndSubindex(op.Vn, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

            Operand res = GetVecA32(vd);

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand de = EmitVectorExtractSx32(context, vd, index + ed * elems, op.Size);
                Operand ne = EmitVectorExtractSx32(context, vn, index + en * elems, op.Size);
                Operand me = EmitVectorExtractSx32(context, vm, index + em * elems, op.Size);

                res = EmitVectorInsert(context, res, emit(de, ne, me), index + ed * elems, op.Size);
            }

            context.Copy(GetVecA32(vd), res);
        }

        public static void EmitVectorUnaryOpZx32(ArmEmitterContext context, Func1I emit)
        {
            OpCode32Simd op = (OpCode32Simd)context.CurrOp;

            (int vm, int em) = GetQuadwordAndSubindex(op.Vm, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

            Operand res = GetVecA32(vd);

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand me = EmitVectorExtractZx32(context, vm, index + em * elems, op.Size);

                res = EmitVectorInsert(context, res, emit(me), index + ed * elems, op.Size);
            }

            context.Copy(GetVecA32(vd), res);
        }

        public static void EmitVectorBinaryOpZx32(ArmEmitterContext context, Func2I emit)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;

            (int vm, int em) = GetQuadwordAndSubindex(op.Vm, op.RegisterSize);
            (int vn, int en) = GetQuadwordAndSubindex(op.Vn, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

            Operand res = GetVecA32(vd);

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractZx32(context, vn, index + en * elems, op.Size);
                Operand me = EmitVectorExtractZx32(context, vm, index + em * elems, op.Size);

                res = EmitVectorInsert(context, res, emit(ne, me), index + ed * elems, op.Size);
            }

            context.Copy(GetVecA32(vd), res);
        }

        public static void EmitVectorTernaryOpZx32(ArmEmitterContext context, Func3I emit)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;

            (int vm, int em) = GetQuadwordAndSubindex(op.Vm, op.RegisterSize);
            (int vn, int en) = GetQuadwordAndSubindex(op.Vn, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

            Operand res = GetVecA32(vd);

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand de = EmitVectorExtractZx32(context, vd, index + ed * elems, op.Size);
                Operand ne = EmitVectorExtractZx32(context, vn, index + en * elems, op.Size);
                Operand me = EmitVectorExtractZx32(context, vm, index + em * elems, op.Size);

                res = EmitVectorInsert(context, res, emit(de, ne, me), index + ed * elems, op.Size);
            }

            context.Copy(GetVecA32(vd), res);
        }

        // VEC BY SCALAR

        public static void EmitVectorByScalarOpF32(ArmEmitterContext context, Func2I emit)
        {
            OpCode32SimdRegElem op = (OpCode32SimdRegElem)context.CurrOp;

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;
            if (op.Size < 2) throw new Exception("FP ops <32 bit unimplemented!");

            int elems = op.GetBytesCount() >> sizeF + 2;

            (int vn, int en) = GetQuadwordAndSubindex(op.Vn, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);
            Operand m = ExtractScalar(context, type, op.Vm);

            Operand res = GetVecA32(vd);

            for (int index = 0; index < elems; index++)
            {
                Operand ne = context.VectorExtract(type, GetVecA32(vn), index + en * elems);

                res = context.VectorInsert(res, emit(ne, m), index + ed * elems);
            }

            context.Copy(GetVecA32(vd), res);
        }

        public static void EmitVectorByScalarOpI32(ArmEmitterContext context, Func2I emit, bool signed)
        {
            OpCode32SimdRegElem op = (OpCode32SimdRegElem)context.CurrOp;

            if (op.Size < 1) throw new Exception("Undefined");
            (int vn, int en) = GetQuadwordAndSubindex(op.Vn, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);
            Operand m = EmitVectorExtract32(context, op.Vm >> (4 - op.Size), op.Vm & ((1 << (4 - op.Size)) - 1), op.Size, signed);

            Operand res = GetVecA32(vd);

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtract32(context, vn, index + en * elems, op.Size, signed);

                res = EmitVectorInsert(context, res, emit(ne, m), index + ed * elems, op.Size);
            }

            context.Copy(GetVecA32(vd), res);
        }

        public static void EmitVectorsByScalarOpF32(ArmEmitterContext context, Func3I emit)
        {
            OpCode32SimdRegElem op = (OpCode32SimdRegElem)context.CurrOp;

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;
            if (op.Size < 2) throw new Exception("FP ops <32 bit unimplemented!");

            int elems = op.GetBytesCount() >> sizeF + 2;

            (int vn, int en) = GetQuadwordAndSubindex(op.Vn, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);
            Operand m = ExtractScalar(context, type, op.Vm);

            Operand res = GetVecA32(vd);

            for (int index = 0; index < elems; index++)
            {
                Operand de = context.VectorExtract(type, GetVecA32(vd), index + ed * elems);
                Operand ne = context.VectorExtract(type, GetVecA32(vn), index + en * elems);

                res = context.VectorInsert(res, emit(de, ne, m), index + ed * elems);
            }

            context.Copy(GetVecA32(vd), res);
        }

        public static void EmitVectorsByScalarOpI32(ArmEmitterContext context, Func3I emit, bool signed)
        {
            OpCode32SimdRegElem op = (OpCode32SimdRegElem)context.CurrOp;

            if (op.Size < 1) throw new Exception("Undefined");
            (int vn, int en) = GetQuadwordAndSubindex(op.Vn, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);
            Operand m = EmitVectorExtract32(context, op.Vm >> (4 - op.Size), op.Vm & ((1 << (4 - op.Size)) - 1), op.Size, signed);

            Operand res = GetVecA32(vd);

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand de = EmitVectorExtract32(context, vd, index + ed * elems, op.Size, signed);
                Operand ne = EmitVectorExtract32(context, vn, index + en * elems, op.Size, signed);

                res = EmitVectorInsert(context, res, emit(de, ne, m), index + ed * elems, op.Size);
            }

            context.Copy(GetVecA32(vd), res);
        }

        // PAIRWISE

        public static void EmitVectorPairwiseOpF32(ArmEmitterContext context, Func2I emit)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;

            if (op.Q)
            {
                throw new Exception("Q mode not supported for pairwise");
            }
            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;

            int elems = op.GetBytesCount() >> (sizeF + 2);
            int pairs = elems >> 1;

            (int vn, int en) = GetQuadwordAndSubindex(op.Vn, op.RegisterSize);
            (int vm, int em) = GetQuadwordAndSubindex(op.Vm, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

            Operand res = GetVecA32(vd);
            Operand mvec = GetVecA32(vm);
            Operand nvec = GetVecA32(vn);

            for (int index = 0; index < pairs; index++)
            {
                int pairIndex = index << 1;

                Operand n1 = context.VectorExtract(type, nvec, pairIndex + en * elems);
                Operand n2 = context.VectorExtract(type, nvec, pairIndex + 1 + en * elems);

                res = context.VectorInsert(res, emit(n1, n2), index + ed * elems);

                Operand m1 = context.VectorExtract(type, mvec, pairIndex + em * elems);
                Operand m2 = context.VectorExtract(type, mvec, pairIndex + 1 + em * elems);

                res = context.VectorInsert(res, emit(m1, m2), index + pairs + ed * elems);
            }

            context.Copy(GetVecA32(vd), res);
        }

        public static void EmitVectorPairwiseOpI32(ArmEmitterContext context, Func2I emit, bool signed)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;

            if (op.Q)
            {
                throw new Exception("Q mode not supported for pairwise");
            }

            int elems = op.GetBytesCount() >> op.Size;
            int pairs = elems >> 1;

            (int vn, int en) = GetQuadwordAndSubindex(op.Vn, op.RegisterSize);
            (int vm, int em) = GetQuadwordAndSubindex(op.Vm, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

            Operand res = GetVecA32(vd);

            for (int index = 0; index < pairs; index++)
            {
                int pairIndex = index << 1;
                Operand n1 = EmitVectorExtract32(context, vn, pairIndex + en * elems, op.Size, signed);
                Operand n2 = EmitVectorExtract32(context, vn, pairIndex + 1 + en * elems, op.Size, signed);

                Operand m1 = EmitVectorExtract32(context, vm, pairIndex + em * elems, op.Size, signed);
                Operand m2 = EmitVectorExtract32(context, vm, pairIndex + 1 + em * elems, op.Size, signed);

                res = EmitVectorInsert(context, res, emit(n1, n2), index + ed * elems, op.Size);
                res = EmitVectorInsert(context, res, emit(m1, m2), index + pairs + ed * elems, op.Size);
            }

            context.Copy(GetVecA32(vd), res);
        }

        // helper func
        public static Operand EmitVectorExtractSx32(ArmEmitterContext context, int reg, int index, int size)
        {
            return EmitVectorExtract32(context, reg, index, size, true);
        }

        public static Operand EmitVectorExtractZx32(ArmEmitterContext context, int reg, int index, int size)
        {
            return EmitVectorExtract32(context, reg, index, size, false);
        }

        public static Operand EmitVectorExtract32(ArmEmitterContext context, int reg, int index, int size, bool signed)
        {
            ThrowIfInvalid(index, size);

            Operand res = null;

            switch (size)
            {
                case 0:
                    res = context.VectorExtract8(GetVec(reg), index);
                    break;

                case 1:
                    res = context.VectorExtract16(GetVec(reg), index);
                    break;

                case 2:
                    res = context.VectorExtract(OperandType.I32, GetVec(reg), index);
                    break;

                case 3:
                    res = context.VectorExtract(OperandType.I64, GetVec(reg), index);
                    break;
            }

            if (signed)
            {
                switch (size)
                {
                    case 0: res = context.SignExtend8(OperandType.I32, res); break;
                    case 1: res = context.SignExtend16(OperandType.I32, res); break;
                }
            }
            else
            {
                switch (size)
                {
                    case 0: res = context.ZeroExtend8(OperandType.I32, res); break;
                    case 1: res = context.ZeroExtend16(OperandType.I32, res); break;
                }
            }

            return res;
        }

    }
}
