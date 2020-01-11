using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Collections.Generic;
using System.Text;
using ARMeilleure.Instructions;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitMemoryHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;
using ARMeilleure.Decoders;
using ARMeilleure.State;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit32
    {
        public static void Clrex(ArmEmitterContext context)
        {
            context.Call(new _Void(NativeInterface.ClearExclusive));
        }

        public static void Ldrex(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, WordSizeLog2, AccessType.LoadZx | AccessType.Exclusive);
        }

        public static void Ldrexb(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, ByteSizeLog2, AccessType.LoadZx | AccessType.Exclusive);
        }

        public static void Ldrexd(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, DWordSizeLog2, AccessType.LoadZx | AccessType.Exclusive);
        }

        public static void Ldrexh(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, HWordSizeLog2, AccessType.LoadZx | AccessType.Exclusive);
        }
        public static void Lda(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, WordSizeLog2, AccessType.LoadZx | AccessType.Ordered);
        }

        public static void Ldab(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, ByteSizeLog2, AccessType.LoadZx | AccessType.Ordered);
        }

        public static void Ldaex(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, WordSizeLog2, AccessType.LoadZx | AccessType.Exclusive | AccessType.Ordered);
        }

        public static void Ldaexb(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, ByteSizeLog2, AccessType.LoadZx | AccessType.Exclusive | AccessType.Ordered);
        }

        public static void Ldaexd(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, DWordSizeLog2, AccessType.LoadZx | AccessType.Exclusive | AccessType.Ordered);
        }

        public static void Ldaexh(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, HWordSizeLog2, AccessType.LoadZx | AccessType.Exclusive | AccessType.Ordered);
        }

        public static void Ldah(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, HWordSizeLog2, AccessType.LoadZx | AccessType.Ordered);
        }

        // stores

        public static void Strex(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, WordSizeLog2, AccessType.Store | AccessType.Exclusive);
        }

        public static void Strexb(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, ByteSizeLog2, AccessType.Store | AccessType.Exclusive);
        }

        public static void Strexd(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, DWordSizeLog2, AccessType.Store | AccessType.Exclusive);
        }

        public static void Strexh(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, HWordSizeLog2, AccessType.Store | AccessType.Exclusive);
        }

        public static void Stl(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, WordSizeLog2, AccessType.Store | AccessType.Ordered);
        }

        public static void Stlb(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, ByteSizeLog2, AccessType.Store | AccessType.Ordered);
        }

        public static void Stlex(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, WordSizeLog2, AccessType.Store | AccessType.Exclusive | AccessType.Ordered);
        }

        public static void Stlexb(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, ByteSizeLog2, AccessType.Store | AccessType.Exclusive | AccessType.Ordered);
        }

        public static void Stlexd(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, DWordSizeLog2, AccessType.Store | AccessType.Exclusive | AccessType.Ordered);
        }

        public static void Stlexh(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, HWordSizeLog2, AccessType.Store | AccessType.Exclusive | AccessType.Ordered);
        }

        public static void Stlh(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, HWordSizeLog2, AccessType.Store | AccessType.Ordered);
        }

        public static void Dmb(ArmEmitterContext context) => EmitBarrier(context);
        public static void Dsb(ArmEmitterContext context) => EmitBarrier(context);

        private static void EmitExLoadOrStore(ArmEmitterContext context, int size, AccessType accType)
        {
            IOpCode32MemEx op = (IOpCode32MemEx)context.CurrOp;

            Operand address = context.Copy(GetIntA32(context, op.Rn));

            var exclusive = (accType & AccessType.Exclusive) != 0;
            var ordered = (accType & AccessType.Ordered) != 0;

            if (ordered)
            {
                EmitBarrier(context);
            }

            if ((accType & AccessType.Load) != 0)
            {
                if (size == DWordSizeLog2)
                {
                    // keep loads atomic - make the call to get the whole region and then decompose it into parts
                    // for the registers.

                    Operand value = EmitExLoad(context, address, exclusive, size);

                    Operand valueLow = context.ConvertI64ToI32(value);

                    valueLow = context.ZeroExtend32(OperandType.I64, valueLow);

                    Operand valueHigh = context.ShiftRightUI(value, Const(32));

                    Operand lblBigEndian = Label();
                    Operand lblEnd = Label();

                    context.BranchIfTrue(lblBigEndian, GetFlag(PState.EFlag));

                    SetIntA32(context, op.Rt, valueLow);
                    SetIntA32(context, op.Rt | 1, valueHigh);

                    context.Branch(lblEnd);

                    context.MarkLabel(lblBigEndian);

                    SetIntA32(context, op.Rt | 1, valueLow);
                    SetIntA32(context, op.Rt, valueHigh);

                    context.MarkLabel(lblEnd);
                }
                else
                {
                    SetIntA32(context, op.Rt, EmitExLoad(context, address, exclusive, size));
                }
            }
            else
            {
                Operand s = null;

                if (size == DWordSizeLog2)
                {
                    //split the result into 2 words (based on endianness)

                    Operand lo = context.ZeroExtend32(OperandType.I64, GetIntA32(context, op.Rt));
                    Operand hi = context.ZeroExtend32(OperandType.I64, GetIntA32(context, op.Rt | 1));
                    Operand toStore = Local(OperandType.I64);

                    Operand lblBigEndian = Label();
                    Operand lblEnd = Label();

                    context.BranchIfTrue(lblBigEndian, GetFlag(PState.EFlag));

                    Operand leResult = context.BitwiseOr(lo, context.ShiftLeft(hi, Const(32)));
                    Operand leS = EmitExStore(context, address, leResult, exclusive, size);
                    if (exclusive) SetIntA32(context, op.Rd, leS);

                    context.Branch(lblEnd);

                    context.MarkLabel(lblBigEndian);

                    Operand beResult = context.BitwiseOr(hi, context.ShiftLeft(lo, Const(32)));
                    Operand beS = EmitExStore(context, address, beResult, exclusive, size);
                    if (exclusive) SetIntA32(context, op.Rd, beS);

                    context.MarkLabel(lblEnd);
                }
                else
                {
                    
                    s = EmitExStore(context, address, context.ZeroExtend32(OperandType.I64, GetIntA32(context, op.Rt)), exclusive, size);
                    // This is only needed for exclusive stores. The function returns 0
                    // when the store is successful, and 1 otherwise.
                    if (exclusive) SetIntA32(context, op.Rd, s);
                }
            }
        }

        private static Operand EmitExLoad(
            ArmEmitterContext context,
            Operand address,
            bool exclusive,
            int size)
        {
            Delegate fallbackMethodDlg = null;

            if (exclusive)
            {
                switch (size)
                {
                    case 0: fallbackMethodDlg = new _U8_U64(NativeInterface.ReadByteExclusive); break;
                    case 1: fallbackMethodDlg = new _U16_U64(NativeInterface.ReadUInt16Exclusive); break;
                    case 2: fallbackMethodDlg = new _U32_U64(NativeInterface.ReadUInt32Exclusive); break;
                    case 3: fallbackMethodDlg = new _U64_U64(NativeInterface.ReadUInt64Exclusive); break;
                    case 4: fallbackMethodDlg = new _V128_U64(NativeInterface.ReadVector128Exclusive); break;
                }
            }
            else
            {
                switch (size)
                {
                    case 0: fallbackMethodDlg = new _U8_U64(NativeInterface.ReadByte); break;
                    case 1: fallbackMethodDlg = new _U16_U64(NativeInterface.ReadUInt16); break;
                    case 2: fallbackMethodDlg = new _U32_U64(NativeInterface.ReadUInt32); break;
                    case 3: fallbackMethodDlg = new _U64_U64(NativeInterface.ReadUInt64); break;
                    case 4: fallbackMethodDlg = new _V128_U64(NativeInterface.ReadVector128); break;
                }
            }

            return context.Call(fallbackMethodDlg, address);
        }

        private static Operand EmitExStore(
            ArmEmitterContext context,
            Operand address,
            Operand value,
            bool exclusive,
            int size)
        {
            if (size < 3)
            {
                value = context.ConvertI64ToI32(value);
            }

            Delegate fallbackMethodDlg = null;

            if (exclusive)
            {
                switch (size)
                {
                    case 0: fallbackMethodDlg = new _S32_U64_U8(NativeInterface.WriteByteExclusive); break;
                    case 1: fallbackMethodDlg = new _S32_U64_U16(NativeInterface.WriteUInt16Exclusive); break;
                    case 2: fallbackMethodDlg = new _S32_U64_U32(NativeInterface.WriteUInt32Exclusive); break;
                    case 3: fallbackMethodDlg = new _S32_U64_U64(NativeInterface.WriteUInt64Exclusive); break;
                    case 4: fallbackMethodDlg = new _S32_U64_V128(NativeInterface.WriteVector128Exclusive); break;
                }

                return context.Call(fallbackMethodDlg, address, value);
            }
            else
            {
                switch (size)
                {
                    case 0: fallbackMethodDlg = new _Void_U64_U8(NativeInterface.WriteByte); break;
                    case 1: fallbackMethodDlg = new _Void_U64_U16(NativeInterface.WriteUInt16); break;
                    case 2: fallbackMethodDlg = new _Void_U64_U32(NativeInterface.WriteUInt32); break;
                    case 3: fallbackMethodDlg = new _Void_U64_U64(NativeInterface.WriteUInt64); break;
                    case 4: fallbackMethodDlg = new _Void_U64_V128(NativeInterface.WriteVector128); break;
                }

                context.Call(fallbackMethodDlg, address, value);

                return null;
            }
        }

        private static void EmitBarrier(ArmEmitterContext context)
        {
            // Note: This barrier is most likely not necessary, and probably
            // doesn't make any difference since we need to do a ton of stuff
            // (software MMU emulation) to read or write anything anyway.
        }
    }
}
