using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Collections.Generic;
using System.Text;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper32;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    using Func2I = Func<Operand, Operand, Operand>;

    static partial class InstEmit32
    {
        public static void Vceq_V(ArmEmitterContext context)
        {
            EmitCmpOpF32(context, SoftFloat32.FPCompareEQ, SoftFloat64.FPCompareEQ, false);
        }

        public static void Vceq_I(ArmEmitterContext context)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;
            EmitCmpOpI32(context, context.ICompareEqual, context.ICompareEqual, false, false);
        }

        public static void Vceq_Z(ArmEmitterContext context)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;
            if (op.F)
            {
                EmitCmpOpF32(context, SoftFloat32.FPCompareEQ, SoftFloat64.FPCompareEQ, true);
            }
            else
            {
                EmitCmpOpI32(context, context.ICompareEqual, context.ICompareEqual, true, false);
            }
        }

        public static void Vcge_V(ArmEmitterContext context)
        {
            EmitCmpOpF32(context, SoftFloat32.FPCompareGE, SoftFloat64.FPCompareGE, false);
        }

        public static void Vcge_I(ArmEmitterContext context)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;
            EmitCmpOpI32(context, context.ICompareGreaterOrEqual, context.ICompareGreaterOrEqualUI, false, !op.U);
        }

        public static void Vcge_Z(ArmEmitterContext context)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;
            if (op.F)
            {
                EmitCmpOpF32(context, SoftFloat32.FPCompareGE, SoftFloat64.FPCompareGE, true);
            } 
            else
            {
                EmitCmpOpI32(context, context.ICompareGreaterOrEqual, context.ICompareGreaterOrEqualUI, true, true);
            }
        }

        public static void Vcgt_V(ArmEmitterContext context)
        {
            EmitCmpOpF32(context, SoftFloat32.FPCompareGT, SoftFloat64.FPCompareGT, false);
        }

        public static void Vcgt_I(ArmEmitterContext context)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;
            EmitCmpOpI32(context, context.ICompareGreater, context.ICompareGreaterUI, false, !op.U);
        }

        public static void Vcgt_Z(ArmEmitterContext context)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;
            if (op.F)
            {
                EmitCmpOpF32(context, SoftFloat32.FPCompareGT, SoftFloat64.FPCompareGT, true);
            }
            else
            {
                EmitCmpOpI32(context, context.ICompareGreater, context.ICompareGreaterUI, true, true);
            }
        }

        public static void Vcle_V(ArmEmitterContext context)
        {
            EmitCmpOpF32(context, SoftFloat32.FPCompareLE, SoftFloat64.FPCompareLE, false);
        }

        public static void Vcle_I(ArmEmitterContext context)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;
            EmitCmpOpI32(context, context.ICompareLessOrEqual, context.ICompareLessOrEqualUI, false, !op.U);
        }

        public static void Vcle_Z(ArmEmitterContext context)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;
            if (op.F)
            {
                EmitCmpOpF32(context, SoftFloat32.FPCompareLE, SoftFloat64.FPCompareLE, true);
            }
            else
            {
                EmitCmpOpI32(context, context.ICompareLessOrEqual, context.ICompareLessOrEqualUI, true, true);
            }
        }

        public static void Vclt_V(ArmEmitterContext context)
        {
            EmitCmpOpF32(context, SoftFloat32.FPCompareLT, SoftFloat64.FPCompareLT, false);
        }

        public static void Vclt_I(ArmEmitterContext context)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;
            EmitCmpOpI32(context, context.ICompareLess, context.ICompareLessUI, false, !op.U);
        }

        public static void Vclt_Z(ArmEmitterContext context)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;
            if (op.F)
            {
                EmitCmpOpF32(context, SoftFloat32.FPCompareLT, SoftFloat64.FPCompareLT, true);
            }
            else
            {
                EmitCmpOpI32(context, context.ICompareLess, context.ICompareLessUI, true, true);
            }
        }

        private static void EmitCmpOpF32(
            ArmEmitterContext context,
            _F32_F32_F32 f32,
            _F64_F64_F64 f64,
            bool zero)
        {
            if (zero)
            {
                EmitVectorUnaryOpF32(context, (m) =>
                {
                    OperandType type = m.Type;

                    if (type == OperandType.FP64) return context.Call(f64, m, new Operand(0.0));
                    else return context.Call(f32, m, new Operand(0.0));
                });
            }
            else
            {
                EmitVectorBinaryOpF32(context, (n, m) =>
                {
                    OperandType type = n.Type;

                    if (type == OperandType.FP64) return context.Call(f64, n, m);
                    else return context.Call(f32, n, m);
                });
            }
        }

        private static Operand ZerosOrOnes(ArmEmitterContext context, Operand fromBool, OperandType baseType)
        {
            return context.ConditionalSelect(fromBool, Const(baseType, -1L), Const(baseType, 0L));
        }

        private static void EmitCmpOpI32(
            ArmEmitterContext context,
            Func2I signedOp,
            Func2I unsignedOp,
            bool zero,
            bool signed)
        {
            if (zero)
            {
                if (signed)
                {
                    EmitVectorUnaryOpSx32(context, (m) =>
                    {
                        OperandType type = m.Type;
                        Operand zeroV = (type == OperandType.I64) ? Const(0L) : Const(0);
                        return ZerosOrOnes(context, signedOp(m, zeroV), type);
                    });
                } 
                else
                {
                    EmitVectorUnaryOpZx32(context, (m) =>
                    {
                        OperandType type = m.Type;
                        Operand zeroV = (type == OperandType.I64) ? Const(0L) : Const(0);
                        return ZerosOrOnes(context, unsignedOp(m, zeroV), type);
                    });
                }
            }
            else
            {
                if (signed)
                {
                    EmitVectorBinaryOpSx32(context, (n, m) => ZerosOrOnes(context, signedOp(n, m), n.Type));
                } 
                else
                {
                    EmitVectorBinaryOpZx32(context, (n, m) => ZerosOrOnes(context, unsignedOp(n, m), n.Type));
                }
            }
        }

        public static void Vcmp(ArmEmitterContext context)
        {
            EmitVcmpOrVcmpe(context, false);
        }

        public static void Vcmpe(ArmEmitterContext context)
        {
            EmitVcmpOrVcmpe(context, true);
        }

        private static void EmitVcmpOrVcmpe(ArmEmitterContext context, bool signalNaNs)
        {
            OpCode32SimdS op = (OpCode32SimdS)context.CurrOp;

            bool cmpWithZero = (op.RawOpCode & (1 << 16)) != 0;
            {
                int fSize = op.Size & 1;
                OperandType type = fSize != 0 ? OperandType.FP64 : OperandType.FP32;

                
                Operand ne = ExtractScalar(context, type, op.Vd);
                Operand me;

                if (cmpWithZero)
                {
                    me = fSize == 0 ? ConstF(0f) : ConstF(0d);
                }
                else
                {
                    me = ExtractScalar(context, type, op.Vm);
                }

                Delegate dlg = op.Size != 0
                    ? (Delegate)new _S32_F64_F64_Bool(SoftFloat64.FPCompare)
                    : (Delegate)new _S32_F32_F32_Bool(SoftFloat32.FPCompare);

                Operand nzcv = context.Call(dlg, ne, me, Const(signalNaNs));

                EmitSetFPSCRFlags(context, nzcv);
            }
        }

        private static void EmitSetFPSCRFlags(ArmEmitterContext context, Operand flags)
        {
            Delegate getDlg = new _U32(NativeInterface.GetFpscr);
            Operand fpscr = context.Call(getDlg);

            fpscr = context.BitwiseOr(context.ShiftLeft(flags, Const(28)), context.BitwiseAnd(fpscr, Const(0x0fffffff)));

            Delegate setDlg = new _Void_U32(NativeInterface.SetFpscr);
            context.Call(setDlg, fpscr);
        }
    }
}
