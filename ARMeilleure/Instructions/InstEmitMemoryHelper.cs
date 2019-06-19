using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Memory;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System;
using System.Reflection;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static class InstEmitMemoryHelper
    {
        private static bool ForceFallback = false;

        private enum Extension
        {
            Zx,
            Sx32,
            Sx64
        }

        public static void EmitLoadZx(EmitterContext context, Operand address, int rt, int size)
        {
            EmitLoad(context, address, Extension.Zx, rt, size);
        }

        public static void EmitLoadSx32(EmitterContext context, Operand address, int rt, int size)
        {
            EmitLoad(context, address, Extension.Sx32, rt, size);
        }

        public static void EmitLoadSx64(EmitterContext context, Operand address, int rt, int size)
        {
            EmitLoad(context, address, Extension.Sx64, rt, size);
        }

        private static void EmitLoad(EmitterContext context, Operand address, Extension ext, int rt, int size)
        {
            bool isSimd = IsSimd(context);

            if ((uint)size > (isSimd ? 4 : 3))
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            if (isSimd)
            {
                if (ForceFallback || !Optimizations.UseSse2 || size < 2)
                {
                    EmitReadVectorFallback(context, address, rt, size);
                }
                else
                {
                    EmitReadVector(context, address, rt, size);
                }
            }
            else
            {
                if (ForceFallback)
                {
                    EmitReadIntFallback(context, address, rt, size);
                }
                else
                {
                    EmitReadInt(context, address, rt, size);
                }
            }

            if (!isSimd)
            {
                Operand value = GetT(context, rt);

                if (ext == Extension.Sx64)
                {
                    value = context.Copy(Local(OperandType.I64), value);
                }

                if (ext == Extension.Sx32 || ext == Extension.Sx64)
                {
                    switch (size)
                    {
                        case 0: value = context.SignExtend8 (value); break;
                        case 1: value = context.SignExtend16(value); break;
                        case 2: value = context.SignExtend32(value); break;
                    }
                }
            }
        }

        public static void EmitStore(EmitterContext context, Operand address, int rt, int size)
        {
            bool isSimd = IsSimd(context);

            if ((uint)size > (isSimd ? 4 : 3))
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            if (isSimd)
            {
                if (ForceFallback || !Optimizations.UseSse2 || size < 2)
                {
                    EmitWriteVectorFallback(context, address, rt, size);
                }
                else
                {
                    EmitWriteVector(context, address, rt, size);
                }
            }
            else
            {
                if (ForceFallback)
                {
                    EmitWriteIntFallback(context, address, rt, size);
                }
                else
                {
                    EmitWriteInt(context, address, rt, size);
                }
            }
        }

        private static bool IsSimd(EmitterContext context)
        {
            return context.CurrOp is IOpCodeSimd &&
                 !(context.CurrOp is OpCodeSimdMemMs ||
                   context.CurrOp is OpCodeSimdMemSs);
        }

        private static void EmitReadInt(EmitterContext context, Operand address, int rt, int size)
        {
            Operand isUnalignedAddr = EmitAddressCheck(context, address, size);

            Operand lblFastPath = Label();
            Operand lblSlowPath = Label();
            Operand lblEnd      = Label();

            context.BranchIfFalse(lblFastPath, isUnalignedAddr);

            context.MarkLabel(lblSlowPath);

            EmitReadIntFallback(context, address, rt, size);

            context.Branch(lblEnd);

            context.MarkLabel(lblFastPath);

            Operand physAddr = EmitPtPointerLoad(context, address, lblSlowPath);

            Operand value = null;

            switch (size)
            {
                case 0: value = context.LoadZx8 (Local(OperandType.I32), physAddr); break;
                case 1: value = context.LoadZx16(Local(OperandType.I32), physAddr); break;
                case 2: value = context.Load    (Local(OperandType.I32), physAddr); break;
                case 3: value = context.Load    (Local(OperandType.I64), physAddr); break;
            }

            context.Copy(GetT(context, rt), value);

            context.MarkLabel(lblEnd);
        }

        private static void EmitReadVector(EmitterContext context, Operand address, int rt, int size)
        {
            Operand isUnalignedAddr = EmitAddressCheck(context, address, size);

            Operand lblFastPath = Label();
            Operand lblSlowPath = Label();
            Operand lblEnd      = Label();

            context.BranchIfFalse(lblFastPath, isUnalignedAddr);

            context.MarkLabel(lblSlowPath);

            EmitReadVectorFallback(context, address, rt, size);

            context.Branch(lblEnd);

            context.MarkLabel(lblFastPath);

            Operand physAddr = EmitPtPointerLoad(context, address, lblSlowPath);

            Operand value = null;

            /*switch (size)
            {
                case 2: context.EmitCall(typeof(Sse), nameof(Sse.LoadScalarVector128));  break;

                case 3:
                {
                    Type[] types = new Type[] { typeof(double*) };

                    context.EmitCall(typeof(Sse2).GetMethod(nameof(Sse2.LoadScalarVector128), types));

                    break;
                }

                case 4: context.EmitCall(typeof(Sse), nameof(Sse.LoadAlignedVector128)); break;

                throw new InvalidOperationException($"Invalid vector load size of {1 << size} bytes.");
            }*/

            context.Copy(GetVec(rt), value);

            context.MarkLabel(lblEnd);
        }

        private static void EmitWriteInt(EmitterContext context, Operand address, int rt, int size)
        {
            Operand isUnalignedAddr = EmitAddressCheck(context, address, size);

            Operand lblFastPath = Label();
            Operand lblSlowPath = Label();
            Operand lblEnd      = Label();

            context.BranchIfFalse(lblFastPath, isUnalignedAddr);

            context.MarkLabel(lblSlowPath);

            EmitWriteIntFallback(context, address, rt, size);

            context.Branch(lblEnd);

            context.MarkLabel(lblFastPath);

            Operand physAddr = EmitPtPointerLoad(context, address, lblSlowPath);

            Operand value = GetT(context, rt);

            if (size < 3)
            {
                value = context.Copy(Local(OperandType.I32), value);
            }

            switch (size)
            {
                case 0: context.Store8 (physAddr, value); break;
                case 1: context.Store16(physAddr, value); break;
                case 2: context.Store  (physAddr, value); break;
                case 3: context.Store  (physAddr, value); break;
            }

            context.MarkLabel(lblEnd);
        }

        private static void EmitWriteVector(EmitterContext context, Operand address, int rt, int size)
        {
            Operand isUnalignedAddr = EmitAddressCheck(context, address, size);

            Operand lblFastPath = Label();
            Operand lblSlowPath = Label();
            Operand lblEnd      = Label();

            context.BranchIfFalse(lblFastPath, isUnalignedAddr);

            context.MarkLabel(lblSlowPath);

            EmitWriteVectorFallback(context, address, rt, size);

            context.Branch(lblEnd);

            context.MarkLabel(lblFastPath);

            Operand physAddr = EmitPtPointerLoad(context, address, lblSlowPath);

            Operand value = GetVec(rt);

            /*switch (size)
            {
                case 2: context.EmitCall(typeof(Sse),  nameof(Sse.StoreScalar));  break;
                case 3: context.EmitCall(typeof(Sse2), nameof(Sse2.StoreScalar)); break;
                case 4: context.EmitCall(typeof(Sse),  nameof(Sse.StoreAligned)); break;

                default: throw new InvalidOperationException($"Invalid vector store size of {1 << size} bytes.");
            }*/

            context.MarkLabel(lblEnd);
        }

        private static Operand EmitAddressCheck(EmitterContext context, Operand address, int size)
        {
            long addressCheckMask = ~(context.Memory.AddressSpaceSize - 1);

            addressCheckMask |= (1u << size) - 1;

            return context.BitwiseAnd(address, Const(addressCheckMask));
        }

        private static Operand EmitPtPointerLoad(EmitterContext context, Operand address, Operand lblFallbackPath)
        {
            Operand pte = Const(context.Memory.PageTable.ToInt64());

            int bit = MemoryManager.PageBits;

            do
            {
                Operand addrPart = context.ShiftRightUI(address, Const(bit));

                bit += context.Memory.PtLevelBits;

                if (bit < context.Memory.AddressSpaceBits)
                {
                    addrPart = context.BitwiseAnd(addrPart, Const((long)context.Memory.PtLevelMask));
                }

                Operand pteOffset = context.ShiftLeft(addrPart, Const(3));

                Operand pteAddress = context.Add(pte, pteOffset);

                pte = context.Load(Local(OperandType.I64), pteAddress);
            }
            while (bit < context.Memory.AddressSpaceBits);

            if (!context.Memory.HasWriteWatchSupport)
            {
                Operand hasFlagSet = context.BitwiseAnd(pte, Const((long)MemoryManager.PteFlagsMask));

                context.BranchIfTrue(lblFallbackPath, hasFlagSet);
            }

            Operand pageOffset = context.BitwiseAnd(address, Const((long)MemoryManager.PageMask));

            Operand physAddr = context.Add(pte, pageOffset);

            return physAddr;
        }

        private static void EmitReadIntFallback(EmitterContext context, Operand address, int rt, int size)
        {
            string fallbackMethodName = null;

            switch (size)
            {
                case 0: fallbackMethodName = nameof(NativeInterface.ReadByte);   break;
                case 1: fallbackMethodName = nameof(NativeInterface.ReadUInt16); break;
                case 2: fallbackMethodName = nameof(NativeInterface.ReadUInt32); break;
                case 3: fallbackMethodName = nameof(NativeInterface.ReadUInt64); break;
            }

            MethodInfo info = typeof(NativeInterface).GetMethod(fallbackMethodName);

            if (context.CurrOp.RegisterSize == RegisterSize.Int32)
            {
                address = context.Copy(Local(OperandType.I64), address);
            }

            context.Copy(GetT(context, rt), context.Call(info, address));
        }

        private static void EmitReadVectorFallback(EmitterContext context, Operand address, int rt, int size)
        {
            string fallbackMethodName = null;

            switch (size)
            {
                case 0: fallbackMethodName = nameof(NativeInterface.ReadVector8);   break;
                case 1: fallbackMethodName = nameof(NativeInterface.ReadVector16);  break;
                case 2: fallbackMethodName = nameof(NativeInterface.ReadVector32);  break;
                case 3: fallbackMethodName = nameof(NativeInterface.ReadVector64);  break;
                case 4: fallbackMethodName = nameof(NativeInterface.ReadVector128); break;
            }

            MethodInfo info = typeof(NativeInterface).GetMethod(fallbackMethodName);

            if (context.CurrOp.RegisterSize == RegisterSize.Int32)
            {
                address = context.Copy(Local(OperandType.I64), address);
            }

            context.Copy(GetVec(rt), context.Call(info, address));
        }

        private static void EmitWriteIntFallback(EmitterContext context, Operand address, int rt, int size)
        {
            string fallbackMethodName = null;

            switch (size)
            {
                case 0: fallbackMethodName = nameof(NativeInterface.WriteByte);   break;
                case 1: fallbackMethodName = nameof(NativeInterface.WriteUInt16); break;
                case 2: fallbackMethodName = nameof(NativeInterface.WriteUInt32); break;
                case 3: fallbackMethodName = nameof(NativeInterface.WriteUInt64); break;
            }

            MethodInfo info = typeof(NativeInterface).GetMethod(fallbackMethodName);

            if (context.CurrOp.RegisterSize == RegisterSize.Int32)
            {
                address = context.Copy(Local(OperandType.I64), address);
            }

            Operand value = GetT(context, rt);

            if (size < 3)
            {
                value = context.Copy(Local(OperandType.I32), value);
            }

            context.Call(info, address, value);
        }

        private static void EmitWriteVectorFallback(EmitterContext context, Operand address, int rt, int size)
        {
            string fallbackMethodName = null;

            switch (size)
            {
                case 0: fallbackMethodName = nameof(NativeInterface.WriteVector8);   break;
                case 1: fallbackMethodName = nameof(NativeInterface.WriteVector16);  break;
                case 2: fallbackMethodName = nameof(NativeInterface.WriteVector32);  break;
                case 3: fallbackMethodName = nameof(NativeInterface.WriteVector64);  break;
                case 4: fallbackMethodName = nameof(NativeInterface.WriteVector128); break;
            }

            MethodInfo info = typeof(NativeInterface).GetMethod(fallbackMethodName);

            if (context.CurrOp.RegisterSize == RegisterSize.Int32)
            {
                address = context.Copy(Local(OperandType.I64), address);
            }

            Operand value = GetVec(rt);

            context.Call(info, address, value);
        }

        private static Operand GetT(EmitterContext context, int rt)
        {
            OpCode op = context.CurrOp;

            if (op is IOpCodeSimd)
            {
                return GetVec(rt);
            }
            else if (op is OpCodeMem opMem)
            {
                bool is32Bits = opMem.Size < 3 && !opMem.Extend64;

                OperandType type = is32Bits ? OperandType.I32 : OperandType.I64;

                if (rt == RegisterConsts.ZeroIndex)
                {
                    return Const(type, 0);
                }

                return Register(rt, RegisterType.Integer, type);
            }
            else
            {
                return GetIntOrZR(context.CurrOp, rt);
            }
        }
    }
}