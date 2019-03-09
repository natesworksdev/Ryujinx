using ChocolArm64.Decoders;
using ChocolArm64.State;
using ChocolArm64.Translation;
using System;
using System.Reflection.Emit;
using System.Runtime.Intrinsics.X86;

using static ChocolArm64.Instructions.InstEmitMemoryHelper;
using static ChocolArm64.Instructions.InstEmitSimdHelper;

namespace ChocolArm64.Instructions
{
    static partial class InstEmit
    {
        private static int StVecTemp0 = ILEmitterCtx.GetVecTempIndex();
        private static int StVecTemp1 = ILEmitterCtx.GetVecTempIndex();
        private static int StVecTemp2 = ILEmitterCtx.GetVecTempIndex();
        private static int StVecTemp3 = ILEmitterCtx.GetVecTempIndex();

        public static void Ld__Vms(ILEmitterCtx context)
        {
            EmitSimdMemMs(context, isLoad: true);
        }

        public static void Ld__Vss(ILEmitterCtx context)
        {
            EmitSimdMemSs(context, isLoad: true);
        }

        public static void St__Vms(ILEmitterCtx context)
        {
            EmitSimdMemMs(context, isLoad: false);
        }

        public static void St__Vss(ILEmitterCtx context)
        {
            EmitSimdMemSs(context, isLoad: false);
        }

        private static void EmitSimdMemMs(ILEmitterCtx context, bool isLoad)
        {
            OpCodeSimdMemMs64 op = (OpCodeSimdMemMs64)context.CurrOp;

            const int vectorSizeLog2 = 4;
            const int vectorSize     = 1 << vectorSizeLog2;

            int offset = 0;

            bool sseOptPath = Optimizations.UseSse2 && op.SElems == 4;

            if (sseOptPath && isLoad)
            {
                //Load as if the data was linear.
                int totalVecs = op.Reps * op.SElems;

                for (int index = 0; index < totalVecs; index++)
                {
                    int rtt = (op.Rt + index) & 0x1f;

                    if (op.RegisterSize == RegisterSize.Simd64 && index >= totalVecs / 2)
                    {
                        EmitVectorZeroAll(context, rtt);
                    }
                    else
                    {
                        context.EmitLdint(op.Rn);
                        context.EmitLdc_I8(offset);

                        context.Emit(OpCodes.Add);

                        EmitReadZxCall(context, vectorSizeLog2);

                        context.EmitStvec(rtt);

                        offset += vectorSize;
                    }
                }

                int structsTotalSize = op.SElems * vectorSize;

                int structSize = op.SElems * (1 << op.Size);

                for (int extensionSize = structSize; extensionSize < structsTotalSize; extensionSize <<= 1)
                {
                    int rt0 = (op.Rt + 0) & 0x1f;
                    int rt1 = (op.Rt + 1) & 0x1f;
                    int rt2 = (op.Rt + 2) & 0x1f;
                    int rt3 = (op.Rt + 3) & 0x1f;

                    int elemSize = op.Size;

                    if (extensionSize > vectorSize)
                    {
                        //The struct elements are spanning two vectors.
                        //We need to skip a vector for the second operand.
                        //To do that, we just invert two registers (rt1 and rt2).
                        context.EmitLdvec(rt1);
                        context.EmitLdvec(rt2);
                        context.EmitStvec(rt1);
                        context.EmitStvec(rt2);

                        elemSize = vectorSizeLog2 - 1;
                    }

                    context.EmitLdvec(rt0);
                    context.EmitLdvec(rt1);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.UnpackLow), GetTypesSflUpk(elemSize)));

                    context.EmitLdvec(rt0);
                    context.EmitLdvec(rt1);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.UnpackHigh), GetTypesSflUpk(elemSize)));

                    context.EmitLdvec(rt2);
                    context.EmitLdvec(rt3);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.UnpackLow), GetTypesSflUpk(elemSize)));

                    context.EmitLdvec(rt2);
                    context.EmitLdvec(rt3);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.UnpackHigh), GetTypesSflUpk(elemSize)));

                    context.EmitStvec(rt3);
                    context.EmitStvec(rt2);
                    context.EmitStvec(rt1);
                    context.EmitStvec(rt0);
                }
            }
            else if (sseOptPath /* && !isLoad */)
            {
                int totalVecs = op.Reps * op.SElems;

                int elemSize = op.Size;

                int rt0 = (op.Rt + 0) & 0x1f;
                int rt1 = (op.Rt + 1) & 0x1f;
                int rt2 = (op.Rt + 2) & 0x1f;
                int rt3 = (op.Rt + 3) & 0x1f;

                context.EmitLdvec(rt0);
                context.EmitLdvec(rt1);

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.UnpackLow), GetTypesSflUpk(elemSize)));

                context.EmitLdvec(rt2);
                context.EmitLdvec(rt3);

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.UnpackLow), GetTypesSflUpk(elemSize)));

                context.EmitLdvec(rt0);
                context.EmitLdvec(rt1);

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.UnpackHigh), GetTypesSflUpk(elemSize)));

                context.EmitLdvec(rt2);
                context.EmitLdvec(rt3);

                context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.UnpackHigh), GetTypesSflUpk(elemSize)));

                context.EmitStvec(StVecTemp3);
                context.EmitStvec(StVecTemp2);
                context.EmitStvec(StVecTemp1);
                context.EmitStvec(StVecTemp0);

                if (elemSize < 3)
                {
                    //This only runs if the struct fits into the vector.
                    //For the case where the struct doesn't fit (it's split across two vectors),
                    //this step is not necessary.
                    elemSize++;

                    context.EmitLdvec(StVecTemp0);
                    context.EmitLdvec(StVecTemp1);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.UnpackLow), GetTypesSflUpk(elemSize)));

                    context.EmitLdvec(StVecTemp0);
                    context.EmitLdvec(StVecTemp1);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.UnpackHigh), GetTypesSflUpk(elemSize)));

                    context.EmitLdvec(StVecTemp2);
                    context.EmitLdvec(StVecTemp3);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.UnpackLow), GetTypesSflUpk(elemSize)));

                    context.EmitLdvec(StVecTemp2);
                    context.EmitLdvec(StVecTemp3);

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.UnpackHigh), GetTypesSflUpk(elemSize)));

                    context.EmitStvec(StVecTemp3);
                    context.EmitStvec(StVecTemp2);
                    context.EmitStvec(StVecTemp1);
                    context.EmitStvec(StVecTemp0);
                }

                //Store as if the data was linear.
                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    totalVecs /= 2;
                }

                for (int index = 0; index < totalVecs; index++)
                {
                    context.EmitLdint(op.Rn);
                    context.EmitLdc_I8(offset);

                    context.Emit(OpCodes.Add);

                    switch (index)
                    {
                        case 0: context.EmitLdvec(StVecTemp0); break;
                        case 1: context.EmitLdvec(StVecTemp1); break;
                        case 2: context.EmitLdvec(StVecTemp2); break;
                        case 3: context.EmitLdvec(StVecTemp3); break;
                    }

                    EmitWriteCall(context, vectorSizeLog2);

                    offset += vectorSize;
                }
            }
            else
            {
                for (int rep   = 0; rep   < op.Reps;   rep++)
                for (int elem  = 0; elem  < op.Elems;  elem++)
                for (int sElem = 0; sElem < op.SElems; sElem++)
                {
                    int rtt = (op.Rt + rep + sElem) & 0x1f;

                    if (isLoad)
                    {
                        context.EmitLdint(op.Rn);
                        context.EmitLdc_I8(offset);

                        context.Emit(OpCodes.Add);

                        EmitReadZxCall(context, op.Size);

                        EmitVectorInsert(context, rtt, elem, op.Size);

                        if (op.RegisterSize == RegisterSize.Simd64 && elem == op.Elems - 1)
                        {
                            EmitVectorZeroUpper(context, rtt);
                        }
                    }
                    else
                    {
                        context.EmitLdint(op.Rn);
                        context.EmitLdc_I8(offset);

                        context.Emit(OpCodes.Add);

                        EmitVectorExtractZx(context, rtt, elem, op.Size);

                        EmitWriteCall(context, op.Size);
                    }

                    offset += 1 << op.Size;
                }
            }

            if (op.WBack)
            {
                EmitSimdMemWBack(context, offset);
            }
        }

        private static void EmitSimdMemSs(ILEmitterCtx context, bool isLoad)
        {
            OpCodeSimdMemSs64 op = (OpCodeSimdMemSs64)context.CurrOp;

            int offset = 0;

            void EmitMemAddress()
            {
                context.EmitLdint(op.Rn);
                context.EmitLdc_I8(offset);

                context.Emit(OpCodes.Add);
            }

            if (op.Replicate)
            {
                //Only loads uses the replicate mode.
                if (!isLoad)
                {
                    throw new InvalidOperationException();
                }

                int bytes = op.GetBitsCount() >> 3;
                int elems = bytes >> op.Size;

                for (int sElem = 0; sElem < op.SElems; sElem++)
                {
                    int rt = (op.Rt + sElem) & 0x1f;

                    for (int index = 0; index < elems; index++)
                    {
                        EmitMemAddress();

                        EmitReadZxCall(context, op.Size);

                        EmitVectorInsert(context, rt, index, op.Size);
                    }

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        EmitVectorZeroUpper(context, rt);
                    }

                    offset += 1 << op.Size;
                }
            }
            else
            {
                for (int sElem = 0; sElem < op.SElems; sElem++)
                {
                    int rt = (op.Rt + sElem) & 0x1f;

                    if (isLoad)
                    {
                        EmitMemAddress();

                        EmitReadZxCall(context, op.Size);

                        EmitVectorInsert(context, rt, op.Index, op.Size);
                    }
                    else
                    {
                        EmitMemAddress();

                        EmitVectorExtractZx(context, rt, op.Index, op.Size);

                        EmitWriteCall(context, op.Size);
                    }

                    offset += 1 << op.Size;
                }
            }

            if (op.WBack)
            {
                EmitSimdMemWBack(context, offset);
            }
        }

        private static void EmitSimdMemWBack(ILEmitterCtx context, int offset)
        {
            OpCodeMemReg64 op = (OpCodeMemReg64)context.CurrOp;

            context.EmitLdint(op.Rn);

            if (op.Rm != RegisterAlias.Zr)
            {
                context.EmitLdint(op.Rm);
            }
            else
            {
                context.EmitLdc_I8(offset);
            }

            context.Emit(OpCodes.Add);

            context.EmitStint(op.Rn);
        }
    }
}