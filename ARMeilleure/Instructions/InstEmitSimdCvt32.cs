using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System;
using System.Diagnostics;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper32;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit32
    {
        private static int FlipVdBits(int vd, bool lowBit)
        {
            if (lowBit)
            {
                //move the low bit to the top
                return ((vd & 0x1) << 4) | (vd >> 1);
            } 
            else
            {
                //move the high bit to the bottom
                return ((vd & 0xf) << 1) | (vd >> 4);
            }
        }

        public static void Vcvt_FD(ArmEmitterContext context)
        {
            OpCode32SimdS op = (OpCode32SimdS)context.CurrOp;

            int vm = op.Vm;
            int vd;
            if (op.Size == 3)
            {
                vd = FlipVdBits(op.Vd, true);
                // double to single
                Operand fp = ExtractScalar(context, OperandType.FP64, vm);

                Operand res = context.ConvertToFP(OperandType.FP32, fp);

                InsertScalar(context, vd, res);

                //Operand res = context.AddIntrinsic(Intrinsic.X86Cvtsd2ss, context.VectorZero(), n);
            }
            else
            {
                vd = FlipVdBits(op.Vd, false);
                // single to double
                Operand fp = ExtractScalar(context, OperandType.FP32, vm);

                Operand res = context.ConvertToFP(OperandType.FP64, fp);

                InsertScalar(context, vd, res);

                //Operand res = context.AddIntrinsic(Intrinsic.X86Cvtss2sd, context.VectorZero(), n);
            }
        }

        public static void Vcvt_FI(ArmEmitterContext context)
        {
            OpCode32SimdCvtFI op = (OpCode32SimdCvtFI)context.CurrOp;

            bool toInteger = (op.Opc2 & 0b100) != 0;

            OperandType floatSize = op.RegisterSize == RegisterSize.Simd64 ? OperandType.FP64 : OperandType.FP32;

            if (toInteger)
            {
                bool unsigned = (op.Opc2 & 1) == 0;
                bool roundWithFpscr = op.Opc == 1;

                Operand toConvert = ExtractScalar(context, floatSize, op.Vm);

                Operand asInteger;

                // TODO: Fast Path
                if (roundWithFpscr)
                {
                    // these need to get the FPSCR value, so it's worth noting we'd need to do a c# call at some point.
                    if (floatSize == OperandType.FP64)
                    {
                        if (unsigned)
                        {
                            asInteger = context.Call(new _U32_F64(SoftFallback.DoubleToUInt32), toConvert);
                        } 
                        else
                        {
                            asInteger = context.Call(new _S32_F64(SoftFallback.DoubleToInt32), toConvert);
                        }
                        
                    } 
                    else
                    {
                        if (unsigned)
                        {
                            asInteger = context.Call(new _U32_F32(SoftFallback.FloatToUInt32), toConvert);
                        } 
                        else
                        {
                            asInteger = context.Call(new _S32_F32(SoftFallback.FloatToInt32), toConvert);
                        }
                    }
                    
                } 
                else
                {
                    // round towards zero
                    if (floatSize == OperandType.FP64)
                    {
                        if (unsigned)
                        {
                            asInteger = context.Call(new _U32_F64(CastDoubleToUInt32), toConvert);
                        }
                        else
                        {
                            asInteger = context.Call(new _S32_F64(CastDoubleToInt32), toConvert);
                        }

                    }
                    else
                    {
                        if (unsigned)
                        {
                            asInteger = context.Call(new _U32_F32(CastFloatToUInt32), toConvert);
                        }
                        else
                        {
                            asInteger = context.Call(new _S32_F32(CastFloatToInt32), toConvert);
                        }
                    }
                }

                InsertScalar(context, op.Vd, asInteger);
            } 
            else
            {
                bool unsigned = op.Opc == 0;

                Operand toConvert = ExtractScalar(context, OperandType.I32, op.Vm);

                Operand asFloat = EmitFPConvert(context, toConvert, floatSize, !unsigned);

                InsertScalar(context, op.Vd, asFloat);
            }
        }

        private static int CastDoubleToInt32(double value)
        {
            return (int)value;
        }

        private static uint CastDoubleToUInt32(double value)
        {
            return (uint)value;
        }
        private static int CastFloatToInt32(float value)
        {
            return (int)value;
        }
        private static uint CastFloatToUInt32(float value)
        {
            return (uint)value;
        }

        private static Operand EmitFPConvert(ArmEmitterContext context, Operand value, OperandType type, bool signed)
        {
            Debug.Assert(value.Type == OperandType.I32 || value.Type == OperandType.I64);

            if (signed)
            {
                return context.ConvertToFP(type, value);
            }
            else
            {
                return context.ConvertToFPUI(type, value);
            }
        }
    }
}
