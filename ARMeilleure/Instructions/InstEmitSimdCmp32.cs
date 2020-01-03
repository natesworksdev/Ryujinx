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
    static partial class InstEmit32
    {
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
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;

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
