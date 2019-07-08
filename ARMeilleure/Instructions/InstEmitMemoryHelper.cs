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
                EmitReadVector(context, address, context.VectorZero(), rt, 0, size);
            }
            else
            {
                EmitReadInt(context, address, rt, size);
            }

            if (!isSimd)
            {
                Operand value = GetIntOrZR(context, rt);

                if (ext == Extension.Sx32 || ext == Extension.Sx64)
                {
                    OperandType destType = ext == Extension.Sx64 ? OperandType.I64 : OperandType.I32;

                    switch (size)
                    {
                        case 0: value = context.SignExtend8 (destType, value); break;
                        case 1: value = context.SignExtend16(destType, value); break;
                        case 2: value = context.SignExtend32(destType, value); break;
                    }
                }

                SetIntOrZR(context, rt, value);
            }
        }

        public static void EmitLoadSimd(
            EmitterContext context,
            Operand address,
            Operand vector,
            int rt,
            int elem,
            int size)
        {
            EmitReadVector(context, address, vector, rt, elem, size);
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
                EmitWriteVector(context, address, rt, 0, size);
            }
            else
            {
                EmitWriteInt(context, address, rt, size);
            }
        }

        public static void EmitStoreSimd(
            EmitterContext context,
            Operand address,
            int rt,
            int elem,
            int size)
        {
            EmitWriteVector(context, address, rt, elem, size);
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
                case 0:
                    value = context.Load8(physAddr);
                    break;

                case 1:
                    value = context.Load16(physAddr);
                    break;

                case 2:
                    value = context.Load(OperandType.I32, physAddr);
                    break;

                case 3:
                    value = context.Load(OperandType.I64, physAddr);
                    break;
            }

            SetIntOrZR(context, rt, value);

            context.MarkLabel(lblEnd);
        }

        private static void EmitReadVector(
            EmitterContext context,
            Operand address,
            Operand vector,
            int rt,
            int elem,
            int size)
        {
            Operand isUnalignedAddr = EmitAddressCheck(context, address, size);

            Operand lblFastPath = Label();
            Operand lblSlowPath = Label();
            Operand lblEnd      = Label();

            context.BranchIfFalse(lblFastPath, isUnalignedAddr);

            context.MarkLabel(lblSlowPath);

            EmitReadVectorFallback(context, address, vector, rt, elem, size);

            context.Branch(lblEnd);

            context.MarkLabel(lblFastPath);

            Operand physAddr = EmitPtPointerLoad(context, address, lblSlowPath);

            Operand value = null;

            switch (size)
            {
                case 0:
                    value = context.VectorInsert8(vector, context.Load8(physAddr), elem);
                    break;

                case 1:
                    value = context.VectorInsert16(vector, context.Load16(physAddr), elem);
                    break;

                case 2:
                    value = context.VectorInsert(vector, context.Load(OperandType.I32, physAddr), elem);
                    break;

                case 3:
                    value = context.VectorInsert(vector, context.Load(OperandType.I64, physAddr), elem);
                    break;

                case 4:
                    value = context.Load(OperandType.V128, physAddr);
                    break;
            }

            context.Copy(GetVec(rt), value);

            context.MarkLabel(lblEnd);
        }

        private static Operand VectorCreate(EmitterContext context, Operand value)
        {
            return context.VectorInsert(context.VectorZero(), value, 0);
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

            Operand value = GetIntOrZR(context, rt);

            if (size < 3)
            {
                value = context.ConvertI64ToI32(value);
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

        private static void EmitWriteVector(
            EmitterContext context,
            Operand address,
            int rt,
            int elem,
            int size)
        {
            Operand isUnalignedAddr = EmitAddressCheck(context, address, size);

            Operand lblFastPath = Label();
            Operand lblSlowPath = Label();
            Operand lblEnd      = Label();

            context.BranchIfFalse(lblFastPath, isUnalignedAddr);

            context.MarkLabel(lblSlowPath);

            EmitWriteVectorFallback(context, address, rt, elem, size);

            context.Branch(lblEnd);

            context.MarkLabel(lblFastPath);

            Operand physAddr = EmitPtPointerLoad(context, address, lblSlowPath);

            Operand value = GetVec(rt);

            switch (size)
            {
                case 0:
                    context.Store8(physAddr, context.VectorExtract8(value, elem));
                    break;

                case 1:
                    context.Store16(physAddr, context.VectorExtract16(value, elem));
                    break;

                case 2:
                    context.Store(physAddr, context.VectorExtract(OperandType.FP32, value, elem));
                    break;

                case 3:
                    context.Store(physAddr, context.VectorExtract(OperandType.FP64, value, elem));
                    break;

                case 4:
                    context.Store(physAddr, value);
                    break;
            }

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

                pte = context.Load(OperandType.I64, pteAddress);
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

            SetIntOrZR(context, rt, context.Call(info, address));
        }

        private static void EmitReadVectorFallback(
            EmitterContext context,
            Operand address,
            Operand vector,
            int rt,
            int elem,
            int size)
        {
            string fallbackMethodName = null;

            switch (size)
            {
                case 0: fallbackMethodName = nameof(NativeInterface.ReadByte);      break;
                case 1: fallbackMethodName = nameof(NativeInterface.ReadUInt16);    break;
                case 2: fallbackMethodName = nameof(NativeInterface.ReadUInt32);    break;
                case 3: fallbackMethodName = nameof(NativeInterface.ReadUInt64);    break;
                case 4: fallbackMethodName = nameof(NativeInterface.ReadVector128); break;
            }

            MethodInfo info = typeof(NativeInterface).GetMethod(fallbackMethodName);

            Operand value = context.Call(info, address);

            if (size < 3)
            {
                value = context.ConvertI64ToI32(value);
            }

            switch (size)
            {
                case 0: value = context.VectorInsert8 (vector, value, elem); break;
                case 1: value = context.VectorInsert16(vector, value, elem); break;
                case 2: value = context.VectorInsert  (vector, value, elem); break;
                case 3: value = context.VectorInsert  (vector, value, elem); break;
            }

            context.Copy(GetVec(rt), value);
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

            Operand value = GetIntOrZR(context, rt);

            if (size < 3)
            {
                value = context.ConvertI64ToI32(value);
            }

            context.Call(info, address, value);
        }

        private static void EmitWriteVectorFallback(
            EmitterContext context,
            Operand address,
            int rt,
            int elem,
            int size)
        {
            string fallbackMethodName = null;

            switch (size)
            {
                case 0: fallbackMethodName = nameof(NativeInterface.WriteByte);      break;
                case 1: fallbackMethodName = nameof(NativeInterface.WriteUInt16);    break;
                case 2: fallbackMethodName = nameof(NativeInterface.WriteUInt32);    break;
                case 3: fallbackMethodName = nameof(NativeInterface.WriteUInt64);    break;
                case 4: fallbackMethodName = nameof(NativeInterface.WriteVector128); break;
            }

            MethodInfo info = typeof(NativeInterface).GetMethod(fallbackMethodName);

            Operand value = null;

            if (size < 4)
            {
                switch (size)
                {
                    case 0:
                        value = context.VectorExtract8(GetVec(rt), elem);
                        break;

                    case 1:
                        value = context.VectorExtract16(GetVec(rt), elem);
                        break;

                    case 2:
                        value = context.VectorExtract(OperandType.I32, GetVec(rt), elem);
                        break;

                    case 3:
                        value = context.VectorExtract(OperandType.I64, GetVec(rt), elem);
                        break;
                }
            }
            else
            {
                value = GetVec(rt);
            }

            context.Call(info, address, value);
        }
    }
}