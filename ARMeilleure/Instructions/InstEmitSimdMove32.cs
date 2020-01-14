using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Collections.Generic;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper32;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit32
    {
        public static void Vmov_I(ArmEmitterContext context)
        {
            EmitVectorImmUnaryOp32(context, (op1) => op1);
        }

        public static void Vmov_GS(ArmEmitterContext context)
        {
            OpCode32SimdMovGp op = (OpCode32SimdMovGp)context.CurrOp;
            Operand vec = GetVecA32(op.Vn >> 2);
            if (op.Op == 1)
            {
                // to general purpose
                Operand value = context.VectorExtract(OperandType.I32, vec, op.Vn & 0x3);
                SetIntA32(context, op.Rt, value);
            } 
            else
            {
                // from general purpose
                Operand value = GetIntA32(context, op.Rt);
                context.Copy(vec, context.VectorInsert(vec, value, op.Vn & 0x3));
            }
        }

        public static void Vmov_G1(ArmEmitterContext context)
        {
            OpCode32SimdMovGpElem op = (OpCode32SimdMovGpElem)context.CurrOp;
            int index = op.Index + ((op.Vd & 1) << (3 - op.Size));
            if (op.Op == 1)
            {
                // to general purpose
                Operand value = EmitVectorExtract32(context, op.Vd >> 1, index, op.Size, !op.U);
                SetIntA32(context, op.Rt, value);
            }
            else
            {
                // from general purpose
                Operand vec = GetVecA32(op.Vd >> 1);
                Operand value = GetIntA32(context, op.Rt);
                context.Copy(vec, EmitVectorInsert(context, vec, value, index, op.Size));
            }
        }

        public static void Vmov_G2(ArmEmitterContext context)
        {
            OpCode32SimdMovGpDouble op = (OpCode32SimdMovGpDouble)context.CurrOp;
            Operand vec = GetVecA32(op.Vm >> 1);
            if (op.Op == 1)
            {
                // to general purpose
                Operand lowValue = context.VectorExtract(OperandType.I32, vec, (op.Vm & 1) << 1);
                SetIntA32(context, op.Rt, lowValue);

                Operand highValue = context.VectorExtract(OperandType.I32, vec, ((op.Vm & 1) << 1) | 1);
                SetIntA32(context, op.Rt2, highValue);
            }
            else
            {
                // from general purpose
                Operand lowValue = GetIntA32(context, op.Rt);
                Operand resultVec = context.VectorInsert(vec, lowValue, (op.Vm & 1) << 1);

                Operand highValue = GetIntA32(context, op.Rt2);
                context.Copy(vec, context.VectorInsert(resultVec, highValue, ((op.Vm & 1) << 1) | 1));
            }
        }

        public static void Vmov_GD(ArmEmitterContext context)
        {
            OpCode32SimdMovGpDouble op = (OpCode32SimdMovGpDouble)context.CurrOp;
            Operand vec = GetVecA32(op.Vm >> 1);
            if (op.Op == 1)
            {
                // to general purpose
                Operand value = context.VectorExtract(OperandType.I64, vec, op.Vm & 1);
                SetIntA32(context, op.Rt, context.ConvertI64ToI32(value));
                SetIntA32(context, op.Rt2, context.ConvertI64ToI32(context.ShiftRightUI(value, Const(32))));
            }
            else
            {
                // from general purpose
                Operand lowValue = GetIntA32(context, op.Rt);
                Operand highValue = GetIntA32(context, op.Rt2);

                Operand value = context.BitwiseOr(
                    context.ZeroExtend32(OperandType.I64, lowValue),
                    context.ShiftLeft(context.ZeroExtend32(OperandType.I64, highValue), Const(32))
                    );

                context.Copy(vec, context.VectorInsert(vec, value, op.Vm & 1));
            }
        }

        public static void Vtbl(ArmEmitterContext context)
        {
            OpCode32SimdTbl op = (OpCode32SimdTbl)context.CurrOp;

            bool extension = op.Opc == 1;

            int elems = op.GetBytesCount() >> op.Size;

            (int vm, int em) = GetQuadwordAndSubindex(op.Vm, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

            int length = op.Length + 1;

            Tuple<int, int>[] tableTuples = new Tuple<int, int>[length];
            for (int i=0; i< length; i++)
            {
                (int vn, int en) = GetQuadwordAndSubindex(op.Vn + i, op.RegisterSize);
                tableTuples[i] = new Tuple<int, int>(vn, en);
            }

            int byteLength = length * 8;

            Operand res = GetVecA32(vd);
            Operand m = GetVecA32(vm);

            for (int index = 0; index < elems; index++)
            {
                Operand selectedIndex = context.ZeroExtend8(OperandType.I32, context.VectorExtract8(m, index + em * elems));

                Operand end = Label();
                Operand inRange = context.ICompareLess(selectedIndex, Const(byteLength));
                Operand elemRes = null; // note: this is I64 for ease of calculation

                // for some reason this branch ruins everything so we do an extract + conditional select instead
                // granted that is slower
                // --- context.BranchIfFalse(end, inRange); ---

                // get indexed byte
                // to simplify (ha) the il, we get bytes from every vector and use a nested conditional select to choose the right result
                // does have to extract `length` times for every element but certainly not as bad as it could be

                // which vector number is the index on
                Operand vecIndex = context.ShiftRightUI(selectedIndex, Const(3));
                // what should we shift by to extract it
                Operand subVecIndexShift = context.ShiftLeft(context.BitwiseAnd(selectedIndex, Const(7)), Const(3));

                for (int i=0; i < length; i++)
                {
                    Tuple<int, int> vectorLocation = tableTuples[i];
                    // get the whole vector, we'll get a byte out of it
                    Operand lookupResult;
                    if (vectorLocation.Item1 == vd)
                    {
                        // result contains the current state of the vector
                        lookupResult = context.VectorExtract(OperandType.I64, res, vectorLocation.Item2);
                    } 
                    else
                    {
                        lookupResult = EmitVectorExtract32(context, vectorLocation.Item1, vectorLocation.Item2, 3, false); //I64
                    }
                    
                    lookupResult = context.ShiftRightUI(lookupResult, subVecIndexShift); // get the relevant byte from this vector

                    if (i == 0)
                    {
                        elemRes = lookupResult; //first result is always default
                    } 
                    else
                    {
                        Operand isThisElem = context.ICompareEqual(vecIndex, Const(i));
                        elemRes = context.ConditionalSelect(isThisElem, lookupResult, elemRes);
                    }
                }

                if (!extension) context.MarkLabel(end);

                Operand fallback = (extension) ? context.ZeroExtend32(OperandType.I64, EmitVectorExtract32(context, vd, index + ed * elems, 0, false)) : Const(0L); 

                res = EmitVectorInsert(context, res, context.ConditionalSelect(inRange, elemRes, fallback), index + ed * elems, 0);

                if (extension) context.MarkLabel(end);
            }

            context.Copy(GetVecA32(vd), res);
        }

        public static void Vtrn(ArmEmitterContext context)
        {
            OpCode32SimdCmpZ op = (OpCode32SimdCmpZ)context.CurrOp;

            int elems = op.GetBytesCount() >> op.Size;
            int pairs = elems >> 1;

            (int vm, int em) = GetQuadwordAndSubindex(op.Vm, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

            bool overlap = vm == vd;

            Operand resD = GetVecA32(vd);
            Operand resM = GetVecA32(vm);

            for (int index = 0; index < pairs; index++)
            {
                int pairIndex = index << 1;
                Operand d2 = EmitVectorExtract32(context, vd, pairIndex + 1 + ed * elems, op.Size, false);
                Operand m1 = EmitVectorExtract32(context, vm, pairIndex + em * elems, op.Size, false);

                resD = EmitVectorInsert(context, resD, m1, pairIndex + 1 + ed * elems, op.Size);
                if (overlap) resM = resD;
                resM = EmitVectorInsert(context, resM, d2, pairIndex + em * elems, op.Size);
                if (overlap) resD = resM;
            }

            context.Copy(GetVecA32(vd), resD);
            if (!overlap) context.Copy(GetVecA32(vm), resM);
        }

        public static void Vzip(ArmEmitterContext context)
        {
            OpCode32SimdCmpZ op = (OpCode32SimdCmpZ)context.CurrOp;

            int elems = op.GetBytesCount() >> op.Size;
            int pairs = elems >> 1;

            (int vm, int em) = GetQuadwordAndSubindex(op.Vm, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

            bool overlap = vm == vd;

            Operand resD = GetVecA32(vd);
            Operand resM = GetVecA32(vm);

            for (int index = 0; index < pairs; index++)
            {
                int pairIndex = index << 1;
                Operand dRowD = EmitVectorExtract32(context, vd, index + ed * elems, op.Size, false);
                Operand mRowD = EmitVectorExtract32(context, vm, index + em * elems, op.Size, false);

                Operand dRowM = EmitVectorExtract32(context, vd, index + ed * elems + pairs, op.Size, false);
                Operand mRowM = EmitVectorExtract32(context, vm, index + em * elems + pairs, op.Size, false);

                resD = EmitVectorInsert(context, resD, dRowD, pairIndex + ed * elems, op.Size);
                resD = EmitVectorInsert(context, resD, mRowD, pairIndex + 1 + ed * elems, op.Size);

                if (overlap) resM = resD;

                resM = EmitVectorInsert(context, resM, dRowM, pairIndex + em * elems, op.Size);
                resM = EmitVectorInsert(context, resM, mRowM, pairIndex + 1 + em * elems, op.Size);

                if (overlap) resD = resM;
            }

            context.Copy(GetVecA32(vd), resD);
            if (!overlap) context.Copy(GetVecA32(vm), resM);
        }

        public static void Vuzp(ArmEmitterContext context)
        {
            OpCode32SimdCmpZ op = (OpCode32SimdCmpZ)context.CurrOp;

            int elems = op.GetBytesCount() >> op.Size;
            int pairs = elems >> 1;

            (int vm, int em) = GetQuadwordAndSubindex(op.Vm, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

            bool overlap = vm == vd;

            Operand resD = GetVecA32(vd);
            Operand resM = GetVecA32(vm);

            for (int index = 0; index < elems; index++)
            {
                Operand dIns, mIns;
                if (index >= pairs)
                {
                    int pind = index - pairs;
                    dIns = EmitVectorExtract32(context, vm, (pind << 1) + em * elems, op.Size, false);
                    mIns = EmitVectorExtract32(context, vm, ((pind << 1) | 1) + em * elems, op.Size, false);
                } 
                else
                {
                    dIns = EmitVectorExtract32(context, vd, (index << 1) + ed * elems, op.Size, false);
                    mIns = EmitVectorExtract32(context, vd, ((index << 1) | 1) + ed * elems, op.Size, false);
                }

                resD = EmitVectorInsert(context, resD, dIns, index + ed * elems, op.Size);
                if (overlap) resM = resD;
                resM = EmitVectorInsert(context, resM, mIns, index + em * elems, op.Size);
                if (overlap) resD = resM;
            }

            context.Copy(GetVecA32(vd), resD);
            if (!overlap) context.Copy(GetVecA32(vm), resM);
        }
    }
}
