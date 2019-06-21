using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Diagnostics;
using System.Reflection;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit
    {
        [Flags]
        private enum AccessType
        {
            None      = 0,
            Ordered   = 1,
            Exclusive = 2,
            OrderedEx = Ordered | Exclusive
        }

        public static void Clrex(EmitterContext context)
        {
            MethodInfo info = typeof(NativeInterface).GetMethod(nameof(NativeInterface.ClearExclusive));

            context.Call(info);
        }

        public static void Dmb(EmitterContext context) => EmitBarrier(context);
        public static void Dsb(EmitterContext context) => EmitBarrier(context);

        public static void Ldar(EmitterContext context)  => EmitLdr(context, AccessType.Ordered);
        public static void Ldaxr(EmitterContext context) => EmitLdr(context, AccessType.OrderedEx);
        public static void Ldxr(EmitterContext context)  => EmitLdr(context, AccessType.Exclusive);
        public static void Ldxp(EmitterContext context)  => EmitLdp(context, AccessType.Exclusive);
        public static void Ldaxp(EmitterContext context) => EmitLdp(context, AccessType.OrderedEx);

        private static void EmitLdr(EmitterContext context, AccessType accType)
        {
            EmitLoadEx(context, accType, pair: false);
        }

        private static void EmitLdp(EmitterContext context, AccessType accType)
        {
            EmitLoadEx(context, accType, pair: true);
        }

        private static void EmitLoadEx(EmitterContext context, AccessType accType, bool pair)
        {
            OpCodeMemEx op = (OpCodeMemEx)context.CurrOp;

            bool ordered   = (accType & AccessType.Ordered)   != 0;
            bool exclusive = (accType & AccessType.Exclusive) != 0;

            if (ordered)
            {
                EmitBarrier(context);
            }

            Operand address = context.Copy(GetIntOrSP(op, op.Rn));

            if (pair)
            {
                //Exclusive loads should be atomic. For pairwise loads, we need to
                //read all the data at once. For a 32-bits pairwise load, we do a
                //simple 64-bits load, for a 128-bits load, we need to call a special
                //method to read 128-bits atomically.
                if (op.Size == 2)
                {
                    Operand value = EmitLoad(context, address, exclusive, 3);

                    Operand valueLow = context.Copy(Local(OperandType.I32), value);

                    valueLow = context.Copy(Local(OperandType.I64), valueLow);

                    Operand valueHigh = context.ShiftRightUI(value, Const(32));

                    SetIntOrZR(context, op.Rt,  valueLow);
                    SetIntOrZR(context, op.Rt2, valueHigh);
                }
                else if (op.Size == 3)
                {
                    Operand value = EmitLoad(context, address, exclusive, 4);

                    Operand valueLow  = context.VectorExtract(value, Local(OperandType.I64), 0);
                    Operand valueHigh = context.VectorExtract(value, Local(OperandType.I64), 1);

                    SetIntOrZR(context, op.Rt,  valueLow);
                    SetIntOrZR(context, op.Rt2, valueHigh);
                }
                else
                {
                    throw new InvalidOperationException($"Invalid load size of {1 << op.Size} bytes.");
                }
            }
            else
            {
                //8, 16, 32 or 64-bits (non-pairwise) load.
                Operand value = EmitLoad(context, address, exclusive, op.Size);

                SetIntOrZR(context, op.Rt, value);
            }
        }

        private static Operand EmitLoad(
            EmitterContext context,
            Operand address,
            bool exclusive,
            int size)
        {
            string fallbackMethodName = null;

            if (exclusive)
            {
                switch (size)
                {
                    case 0: fallbackMethodName = nameof(NativeInterface.ReadByteExclusive);      break;
                    case 1: fallbackMethodName = nameof(NativeInterface.ReadUInt16Exclusive);    break;
                    case 2: fallbackMethodName = nameof(NativeInterface.ReadUInt32Exclusive);    break;
                    case 3: fallbackMethodName = nameof(NativeInterface.ReadUInt64Exclusive);    break;
                    case 4: fallbackMethodName = nameof(NativeInterface.ReadVector128Exclusive); break;
                }
            }
            else
            {
                switch (size)
                {
                    case 0: fallbackMethodName = nameof(NativeInterface.ReadByte);      break;
                    case 1: fallbackMethodName = nameof(NativeInterface.ReadUInt16);    break;
                    case 2: fallbackMethodName = nameof(NativeInterface.ReadUInt32);    break;
                    case 3: fallbackMethodName = nameof(NativeInterface.ReadUInt64);    break;
                    case 4: fallbackMethodName = nameof(NativeInterface.ReadVector128); break;
                }
            }

            MethodInfo info = typeof(NativeInterface).GetMethod(fallbackMethodName);

            return context.Call(info, address);
        }

        public static void Pfrm(EmitterContext context)
        {
            //Memory Prefetch, execute as no-op.
        }

        public static void Stlr(EmitterContext context)  => EmitStr(context, AccessType.Ordered);
        public static void Stlxr(EmitterContext context) => EmitStr(context, AccessType.OrderedEx);
        public static void Stxr(EmitterContext context)  => EmitStr(context, AccessType.Exclusive);
        public static void Stxp(EmitterContext context)  => EmitStp(context, AccessType.Exclusive);
        public static void Stlxp(EmitterContext context) => EmitStp(context, AccessType.OrderedEx);

        private static void EmitStr(EmitterContext context, AccessType accType)
        {
            EmitStoreEx(context, accType, pair: false);
        }

        private static void EmitStp(EmitterContext context, AccessType accType)
        {
            EmitStoreEx(context, accType, pair: true);
        }

        private static void EmitStoreEx(EmitterContext context, AccessType accType, bool pair)
        {
            OpCodeMemEx op = (OpCodeMemEx)context.CurrOp;

            bool ordered   = (accType & AccessType.Ordered)   != 0;
            bool exclusive = (accType & AccessType.Exclusive) != 0;

            if (ordered)
            {
                EmitBarrier(context);
            }

            Operand address = context.Copy(GetIntOrSP(op, op.Rn));

            Operand t = GetIntOrZR(op, op.Rt);

            Operand s = null;

            if (pair)
            {
                Debug.Assert(op.Size == 2 || op.Size == 3, "Invalid size for pairwise store.");

                Operand t2 = GetIntOrZR(op, op.Rt2);

                Operand value;

                if (op.Size == 2)
                {
                    value = context.BitwiseOr(t, context.ShiftLeft(t2, Const(32)));
                }
                else /* if (op.Size == 3) */
                {
                    value = context.VectorInsert(context.VectorZero(), t,  0);
                    value = context.VectorInsert(value,                t2, 1);
                }

                s = EmitStore(context, address, value, exclusive, op.Size + 1);
            }
            else
            {
                s = EmitStore(context, address, t, exclusive, op.Size);
            }

            if (s != null)
            {
                //This is only needed for exclusive stores. The function returns 0
                //when the store is successful, and 1 otherwise.
                SetIntOrZR(context, op.Rs, s);
            }
        }

        private static Operand EmitStore(
            EmitterContext context,
            Operand address,
            Operand value,
            bool exclusive,
            int size)
        {
            if (size < 3)
            {
                value = context.Copy(Local(OperandType.I32), value);
            }

            string fallbackMethodName = null;

            if (exclusive)
            {
                switch (size)
                {
                    case 0: fallbackMethodName = nameof(NativeInterface.WriteByteExclusive);      break;
                    case 1: fallbackMethodName = nameof(NativeInterface.WriteUInt16Exclusive);    break;
                    case 2: fallbackMethodName = nameof(NativeInterface.WriteUInt32Exclusive);    break;
                    case 3: fallbackMethodName = nameof(NativeInterface.WriteUInt64Exclusive);    break;
                    case 4: fallbackMethodName = nameof(NativeInterface.WriteVector128Exclusive); break;
                }

                MethodInfo info = typeof(NativeInterface).GetMethod(fallbackMethodName);

                return context.Call(info, address, value);
            }
            else
            {
                switch (size)
                {
                    case 0: fallbackMethodName = nameof(NativeInterface.WriteByte);      break;
                    case 1: fallbackMethodName = nameof(NativeInterface.WriteUInt16);    break;
                    case 2: fallbackMethodName = nameof(NativeInterface.WriteUInt32);    break;
                    case 3: fallbackMethodName = nameof(NativeInterface.WriteUInt64);    break;
                    case 4: fallbackMethodName = nameof(NativeInterface.WriteVector128); break;
                }

                MethodInfo info = typeof(NativeInterface).GetMethod(fallbackMethodName);

                return null;
            }
        }

        private static void EmitBarrier(EmitterContext context)
        {
            //Note: This barrier is most likely not necessary, and probably
            //doesn't make any difference since we need to do a ton of stuff
            //(software MMU emulation) to read or write anything anyway.
        }
    }
}