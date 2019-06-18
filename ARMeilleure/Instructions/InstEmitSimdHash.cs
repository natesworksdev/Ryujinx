using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System.Reflection;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit
    {
#region "Sha1"
        public static void Sha1c_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand d = GetVec(op.Rd);

            Operand ne = context.VectorExtract(GetVec(op.Rn), Local(OperandType.I32), 0);

            Operand m = GetVec(op.Rm);

            MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.HashChoose));

            Operand res = context.Call(info, d, ne, m);

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Sha1h_V(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand ne = context.VectorExtract(GetVec(op.Rn), Local(OperandType.I32), 0);

            MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.FixedRotate));

            Operand res = context.Call(info, ne);

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Sha1m_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand d = GetVec(op.Rd);

            Operand ne = context.VectorExtract(GetVec(op.Rn), Local(OperandType.I32), 0);

            Operand m = GetVec(op.Rm);

            MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.HashMajority));

            Operand res = context.Call(info, d, ne, m);

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Sha1p_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand d = GetVec(op.Rd);

            Operand ne = context.VectorExtract(GetVec(op.Rn), Local(OperandType.I32), 0);

            Operand m = GetVec(op.Rm);

            MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.HashParity));

            Operand res = context.Call(info, d, ne, m);

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Sha1su0_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand d = GetVec(op.Rd);
            Operand n = GetVec(op.Rn);
            Operand m = GetVec(op.Rm);

            MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.Sha1SchedulePart1));

            Operand res = context.Call(info, d, n, m);

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Sha1su1_V(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand d = GetVec(op.Rd);
            Operand n = GetVec(op.Rn);

            MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.Sha1SchedulePart2));

            Operand res = context.Call(info, d, n);

            context.Copy(GetVec(op.Rd), res);
        }
#endregion

#region "Sha256"
        public static void Sha256h_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand d = GetVec(op.Rd);
            Operand n = GetVec(op.Rn);
            Operand m = GetVec(op.Rm);

            MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.HashLower));

            Operand res = context.Call(info, d, n, m);

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Sha256h2_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand d = GetVec(op.Rd);
            Operand n = GetVec(op.Rn);
            Operand m = GetVec(op.Rm);

            MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.HashUpper));

            Operand res = context.Call(info, d, n, m);

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Sha256su0_V(EmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand d = GetVec(op.Rd);
            Operand n = GetVec(op.Rn);

            MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.Sha256SchedulePart1));

            Operand res = context.Call(info, d, n);

            context.Copy(GetVec(op.Rd), res);
        }

        public static void Sha256su1_V(EmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand d = GetVec(op.Rd);
            Operand n = GetVec(op.Rn);
            Operand m = GetVec(op.Rm);

            MethodInfo info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.Sha256SchedulePart2));

            Operand res = context.Call(info, d, n, m);

            context.Copy(GetVec(op.Rd), res);
        }
#endregion
    }
}
