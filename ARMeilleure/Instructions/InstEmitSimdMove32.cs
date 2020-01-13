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

        public static void Vtrn(ArmEmitterContext context)
        {
            OpCode32SimdCmpZ op = (OpCode32SimdCmpZ)context.CurrOp;

            int elems = op.GetBytesCount() >> op.Size;
            int pairs = elems >> 1;

            (int vm, int em) = GetQuadwordAndSubindex(op.Vm, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

            Operand resD = GetVecA32(vd);
            Operand resM = GetVecA32(vm);

            for (int index = 0; index < pairs; index++)
            {
                int pairIndex = index << 1;
                Operand d2 = EmitVectorExtract32(context, vd, pairIndex + 1 + ed * elems, op.Size, false);
                Operand m1 = EmitVectorExtract32(context, vm, pairIndex + em * elems, op.Size, false);

                resD = EmitVectorInsert(context, resD, m1, pairIndex + 1 + ed * elems, op.Size);
                resM = EmitVectorInsert(context, resM, d2, pairIndex + em * elems, op.Size);
            }

            context.Copy(GetVecA32(vd), resD);
            context.Copy(GetVecA32(vm), resM);
        }

        public static void Vzip(ArmEmitterContext context)
        {
            OpCode32SimdCmpZ op = (OpCode32SimdCmpZ)context.CurrOp;

            int elems = op.GetBytesCount() >> op.Size;
            int pairs = elems >> 1;

            (int vm, int em) = GetQuadwordAndSubindex(op.Vm, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

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

                resM = EmitVectorInsert(context, resM, dRowM, pairIndex + em * elems, op.Size);
                resM = EmitVectorInsert(context, resM, mRowM, pairIndex + 1 + em * elems, op.Size);
            }

            context.Copy(GetVecA32(vd), resD);
            context.Copy(GetVecA32(vm), resM);
        }

        public static void Vuzp(ArmEmitterContext context)
        {
            OpCode32SimdCmpZ op = (OpCode32SimdCmpZ)context.CurrOp;

            int elems = op.GetBytesCount() >> op.Size;
            int pairs = elems >> 1;

            (int vm, int em) = GetQuadwordAndSubindex(op.Vm, op.RegisterSize);
            (int vd, int ed) = GetQuadwordAndSubindex(op.Vd, op.RegisterSize);

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
                resM = EmitVectorInsert(context, resM, mIns, index + em * elems, op.Size);
            }

            context.Copy(GetVecA32(vd), resD);
            context.Copy(GetVecA32(vm), resM);
        }
    }
}
