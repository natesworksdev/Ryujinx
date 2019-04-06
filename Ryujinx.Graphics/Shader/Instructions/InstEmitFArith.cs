using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation;
using System;

using static Ryujinx.Graphics.Shader.Instructions.InstEmitAluHelper;
using static Ryujinx.Graphics.Shader.Instructions.InstEmitHelper;
using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Instructions
{
    static partial class InstEmit
    {
        public static void Fadd(EmitterContext context)
        {
            IOpCodeFArith op = (IOpCodeFArith)context.CurrOp;

            bool absoluteA = op.AbsoluteA, absoluteB, negateA, negateB;

            if (op is OpCodeFArithImm32)
            {
                negateB   = op.RawOpCode.Extract(53);
                negateA   = op.RawOpCode.Extract(56);
                absoluteB = op.RawOpCode.Extract(57);
            }
            else
            {
                negateB   = op.RawOpCode.Extract(45);
                negateA   = op.RawOpCode.Extract(48);
                absoluteB = op.RawOpCode.Extract(49);
            }

            Operand srcA = context.FPAbsNeg(GetSrcA(context), absoluteA, negateA);
            Operand srcB = context.FPAbsNeg(GetSrcB(context), absoluteB, negateB);

            Operand dest = GetDest(context);

            context.Copy(dest, context.FPSaturate(context.FPAdd(srcA, srcB), op.Saturate));

            SetFPZnFlags(context, dest, op.SetCondCode);
        }

        public static void Ffma(EmitterContext context)
        {
            IOpCodeFArith op = (IOpCodeFArith)context.CurrOp;

            bool negateB = op.RawOpCode.Extract(48);
            bool negateC = op.RawOpCode.Extract(49);

            Operand srcA = GetSrcA(context);

            Operand srcB = context.FPNegate(GetSrcB(context), negateB);
            Operand srcC = context.FPNegate(GetSrcC(context), negateC);

            Operand dest = GetDest(context);

            context.Copy(dest, context.FPSaturate(context.FPFusedMultiplyAdd(srcA, srcB, srcC), op.Saturate));

            SetFPZnFlags(context, dest, op.SetCondCode);
        }

        public static void Fmnmx(EmitterContext context)
        {
            IOpCodeFArith op = (IOpCodeFArith)context.CurrOp;

            bool absoluteA = op.AbsoluteA;
            bool negateB   = op.RawOpCode.Extract(45);
            bool negateA   = op.RawOpCode.Extract(48);
            bool absoluteB = op.RawOpCode.Extract(49);

            Operand srcA = context.FPAbsNeg(GetSrcA(context), absoluteA, negateA);
            Operand srcB = context.FPAbsNeg(GetSrcB(context), absoluteB, negateB);

            Operand resMin = context.FPMinimum(srcA, srcB);
            Operand resMax = context.FPMaximum(srcA, srcB);

            Operand pred = GetPredicate39(context);

            Operand dest = GetDest(context);

            context.Copy(dest, context.ConditionalSelect(pred, resMin, resMax));

            SetFPZnFlags(context, dest, op.SetCondCode);
        }

        public static void Fmul(EmitterContext context)
        {
            IOpCodeFArith op = (IOpCodeFArith)context.CurrOp;

            bool negateB = !(op is OpCodeFArithImm32) && op.RawOpCode.Extract(48);

            Operand srcA = GetSrcA(context);

            Operand srcB = context.FPNegate(GetSrcB(context), negateB);

            switch (op.Scale)
            {
                case FmulScale.None: break;

                case FmulScale.Divide2:   srcA = context.FPDivide  (srcA, ConstF(2)); break;
                case FmulScale.Divide4:   srcA = context.FPDivide  (srcA, ConstF(4)); break;
                case FmulScale.Divide8:   srcA = context.FPDivide  (srcA, ConstF(8)); break;
                case FmulScale.Multiply2: srcA = context.FPMultiply(srcA, ConstF(2)); break;
                case FmulScale.Multiply4: srcA = context.FPMultiply(srcA, ConstF(4)); break;
                case FmulScale.Multiply8: srcA = context.FPMultiply(srcA, ConstF(8)); break;

                default: break; //TODO: Warning.
            }

            Operand dest = GetDest(context);

            context.Copy(dest, context.FPSaturate(context.FPMultiply(srcA, srcB), op.Saturate));

            SetFPZnFlags(context, dest, op.SetCondCode);
        }

        public static void Fset(EmitterContext context)
        {
            OpCodeSet op = (OpCodeSet)context.CurrOp;

            Condition cmpOp = (Condition)op.RawOpCode.Extract(48, 4);

            bool negateA   = op.RawOpCode.Extract(43);
            bool absoluteB = op.RawOpCode.Extract(44);
            bool boolFloat = op.RawOpCode.Extract(52);
            bool negateB   = op.RawOpCode.Extract(53);
            bool absoluteA = op.RawOpCode.Extract(54);

            Operand srcA = context.FPAbsNeg(GetSrcA(context), absoluteA, negateA);
            Operand srcB = context.FPAbsNeg(GetSrcB(context), absoluteB, negateB);

            Operand res = GetFPComparison(context, cmpOp, srcA, srcB);

            Operand pred = GetPredicate39(context);

            res = GetPredLogicalOp(context, op.LogicalOp, res, pred);

            Operand dest = GetDest(context);

            if (boolFloat)
            {
                context.Copy(dest, context.ConditionalSelect(res, ConstF(1), Const(0)));
            }
            else
            {
                context.Copy(dest, res);
            }

            //TODO: CC, X
        }

        public static void Fsetp(EmitterContext context)
        {
            OpCodeSet op = (OpCodeSet)context.CurrOp;

            Condition cmpOp = (Condition)op.RawOpCode.Extract(48, 4);

            bool absoluteA = op.RawOpCode.Extract(7);
            bool negateA   = op.RawOpCode.Extract(43);
            bool absoluteB = op.RawOpCode.Extract(44);

            Operand srcA = context.FPAbsNeg  (GetSrcA(context), absoluteA, negateA);
            Operand srcB = context.FPAbsolute(GetSrcB(context), absoluteB);

            Operand p0Res = GetFPComparison(context, cmpOp, srcA, srcB);

            Operand p1Res = context.BitwiseNot(p0Res);

            Operand pred = GetPredicate39(context);

            p0Res = GetPredLogicalOp(context, op.LogicalOp, p0Res, pred);
            p1Res = GetPredLogicalOp(context, op.LogicalOp, p1Res, pred);

            context.Copy(Register(op.Predicate3), p0Res);
            context.Copy(Register(op.Predicate0), p1Res);
        }

        public static void Hadd2(EmitterContext context)
        {
            Hadd2Hmul2Impl(context, isAdd: true);
        }

        public static void Hmul2(EmitterContext context)
        {
            Hadd2Hmul2Impl(context, isAdd: false);
        }

        private static void Hadd2Hmul2Impl(EmitterContext context, bool isAdd)
        {
            OpCode op = context.CurrOp;

            bool saturate = op.RawOpCode.Extract(op is OpCodeAluImm32 ? 52 : 32);

            Operand[] srcA = GetHalfSrcA(context);
            Operand[] srcB = GetHalfSrcB(context);

            Operand[] res = new Operand[2];

            for (int index = 0; index < res.Length; index++)
            {
                if (isAdd)
                {
                    res[index] = context.FPAdd(srcA[index], srcB[index]);
                }
                else
                {
                    res[index] = context.FPMultiply(srcA[index], srcB[index]);
                }

                res[index] = context.FPSaturate(res[index], saturate);
            }

            context.Copy(GetDest(context), GetHalfPacked(context, res));
        }

        public static void Mufu(EmitterContext context)
        {
            IOpCodeFArith op = (IOpCodeFArith)context.CurrOp;

            bool negateB = op.RawOpCode.Extract(48);

            Operand res = context.FPAbsNeg(GetSrcA(context), op.AbsoluteA, negateB);

            MufuOperation subOp = (MufuOperation)context.CurrOp.RawOpCode.Extract(20, 4);

            switch (subOp)
            {
                case MufuOperation.Cosine:
                    res = context.FPCosine(res);
                    break;

                case MufuOperation.Sine:
                    res = context.FPSine(res);
                    break;

                case MufuOperation.ExponentB2:
                    res = context.FPExponentB2(res);
                    break;

                case MufuOperation.LogarithmB2:
                    res = context.FPLogarithmB2(res);
                    break;

                case MufuOperation.Reciprocal:
                    res = context.FPReciprocal(res);
                    break;

                case MufuOperation.ReciprocalSquareRoot:
                    res = context.FPReciprocalSquareRoot(res);
                    break;

                case MufuOperation.SquareRoot:
                    res = context.FPSquareRoot(res);
                    break;

                default: /* TODO */ break;
            }

            context.Copy(GetDest(context), context.FPSaturate(res, op.Saturate));
        }

        private static Operand[] GetHalfSrcA(EmitterContext context)
        {
            OpCode op = context.CurrOp;

            bool absoluteA = false, negateA = false;

            if (op is IOpCodeCbuf || op is IOpCodeImm)
            {
                negateA   = op.RawOpCode.Extract(43);
                absoluteA = op.RawOpCode.Extract(44);
            }
            else if (op is IOpCodeReg)
            {
                absoluteA = op.RawOpCode.Extract(44);
            }
            else if (op is OpCodeAluImm32 && op.Emitter == Hadd2)
            {
                negateA = op.RawOpCode.Extract(56);
            }

            FPHalfSwizzle swizzle = (FPHalfSwizzle)context.CurrOp.RawOpCode.Extract(47, 2);

            Operand[] operands = GetHalfSources(context, GetSrcA(context), swizzle);

            return FPAbsNeg(context, operands, absoluteA, negateA);
        }

        private static Operand[] GetHalfSrcB(EmitterContext context)
        {
            OpCode op = context.CurrOp;

            FPHalfSwizzle swizzle = FPHalfSwizzle.FP16;

            if (!(op is IOpCodeImm))
            {
                swizzle = (FPHalfSwizzle)op.RawOpCode.Extract(28, 2);
            }

            bool absoluteB = false, negateB = false;

            if (op is IOpCodeReg)
            {
                absoluteB = op.RawOpCode.Extract(30);
                negateB   = op.RawOpCode.Extract(31);
            }
            else if (op is IOpCodeCbuf)
            {
                absoluteB = op.RawOpCode.Extract(54);
            }

            Operand[] operands = GetHalfSources(context, GetSrcB(context), swizzle);

            return FPAbsNeg(context, operands, absoluteB, negateB);
        }

        private static Operand[] GetHalfSources(EmitterContext context, Operand src, FPHalfSwizzle swizzle)
        {
            switch (swizzle)
            {
                case FPHalfSwizzle.FP16:
                    return new Operand[]
                    {
                        context.UnpackHalf2x16Low (src),
                        context.UnpackHalf2x16High(src)
                    };

                case FPHalfSwizzle.FP32: return new Operand[] { src, src };

                case FPHalfSwizzle.DupH0:
                    return new Operand[]
                    {
                        context.UnpackHalf2x16Low(src),
                        context.UnpackHalf2x16Low(src)
                    };

                case FPHalfSwizzle.DupH1:
                    return new Operand[]
                    {
                        context.UnpackHalf2x16High(src),
                        context.UnpackHalf2x16High(src)
                    };
            }

            throw new ArgumentException($"Invalid swizzle \"{swizzle}\".");
        }

        private static Operand[] FPAbsNeg(EmitterContext context, Operand[] operands, bool abs, bool neg)
        {
            for (int index = 0; index < operands.Length; index++)
            {
                operands[index] = context.FPAbsNeg(operands[index], abs, neg);
            }

            return operands;
        }

        private static Operand GetHalfPacked(EmitterContext context, Operand[] results)
        {
            OpCode op = context.CurrOp;

            FPHalfSwizzle swizzle = FPHalfSwizzle.FP16;

            if (!(op is OpCodeAluImm32))
            {
                swizzle = (FPHalfSwizzle)context.CurrOp.RawOpCode.Extract(49, 2);
            }

            switch (swizzle)
            {
                case FPHalfSwizzle.FP16: return context.PackHalf2x16(results[0], results[1]);

                case FPHalfSwizzle.FP32: return results[0];

                case FPHalfSwizzle.DupH0:
                {
                    Operand h1 = GetHalfDest(context, isHigh: true);

                    return context.PackHalf2x16(results[0], h1);
                }

                case FPHalfSwizzle.DupH1:
                {
                    Operand h0 = GetHalfDest(context, isHigh: false);

                    return context.PackHalf2x16(h0, results[1]);
                }
            }

            throw new ArgumentException($"Invalid swizzle \"{swizzle}\".");
        }

        private static Operand GetHalfDest(EmitterContext context, bool isHigh)
        {
            if (isHigh)
            {
                return context.UnpackHalf2x16High(GetDest(context));
            }
            else
            {
                return context.UnpackHalf2x16Low(GetDest(context));
            }
        }

        private static Operand GetFPComparison(
            EmitterContext context,
            Condition      cond,
            Operand        srcA,
            Operand        srcB)
        {
            Operand res;

            if (cond == Condition.Always)
            {
                res = Const(IrConsts.True);
            }
            else if (cond == Condition.Never)
            {
                res = Const(IrConsts.False);
            }
            else if (cond == Condition.Nan || cond == Condition.Number)
            {
                res = context.BitwiseOr(context.IsNan(srcA), context.IsNan(srcB));

                if (cond == Condition.Number)
                {
                    res = context.BitwiseNot(res);
                }
            }
            else
            {
                Instruction inst;

                switch (cond & ~Condition.Nan)
                {
                    case Condition.Less:           inst = Instruction.CompareLess;           break;
                    case Condition.Equal:          inst = Instruction.CompareEqual;          break;
                    case Condition.LessOrEqual:    inst = Instruction.CompareLessOrEqual;    break;
                    case Condition.Greater:        inst = Instruction.CompareGreater;        break;
                    case Condition.NotEqual:       inst = Instruction.CompareNotEqual;       break;
                    case Condition.GreaterOrEqual: inst = Instruction.CompareGreaterOrEqual; break;

                    default: throw new InvalidOperationException($"Unexpected condition \"{cond}\".");
                }

                res = context.Add(inst | Instruction.FP, Local(), srcA, srcB);

                if ((cond & Condition.Nan) != 0)
                {
                    res = context.BitwiseOr(res, context.IsNan(srcA));
                    res = context.BitwiseOr(res, context.IsNan(srcB));
                }
            }

            return res;
        }

        private static void SetFPZnFlags(EmitterContext context, Operand dest, bool setCC)
        {
            if (setCC)
            {
                context.Copy(GetZF(context), context.FPCompareEqual(dest, ConstF(0)));
                context.Copy(GetNF(context), context.FPCompareLess (dest, ConstF(0)));
            }
        }
    }
}