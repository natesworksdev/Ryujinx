using System;

using static Ryujinx.Graphics.Gal.Shader.ShaderDecodeHelper;

namespace Ryujinx.Graphics.Gal.Shader
{
    static partial class ShaderDecode
    {
        public static void Bfe_C(ShaderIrBlock block, long opCode, int position)
        {
            EmitBfe(block, opCode, ShaderOper.Cr);
        }

        public static void Bfe_I(ShaderIrBlock block, long opCode, int position)
        {
            EmitBfe(block, opCode, ShaderOper.Imm);
        }

        public static void Bfe_R(ShaderIrBlock block, long opCode, int position)
        {
            EmitBfe(block, opCode, ShaderOper.Rr);
        }

        public static void Fadd_C(ShaderIrBlock block, long opCode, int position)
        {
            EmitFadd(block, opCode, ShaderOper.Cr);
        }

        public static void Fadd_I(ShaderIrBlock block, long opCode, int position)
        {
            EmitFadd(block, opCode, ShaderOper.Immf);
        }

        public static void Fadd_I32(ShaderIrBlock block, long opCode, int position)
        {
            ShaderIrNode operA = opCode.Gpr8();
            ShaderIrNode operB = opCode.Immf32_20();

            bool negB = opCode.Read(53);
            bool absA = opCode.Read(54);
            bool negA = opCode.Read(56);
            bool absB = opCode.Read(57);

            operA = GetAluFabsFneg(operA, absA, negA);
            operB = GetAluFabsFneg(operB, absB, negB);

            ShaderIrOp op = new ShaderIrOp(ShaderIrInst.Fadd, operA, operB);

            block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), op)));
        }

        public static void Fadd_R(ShaderIrBlock block, long opCode, int position)
        {
            EmitFadd(block, opCode, ShaderOper.Rr);
        }

        public static void Ffma_CR(ShaderIrBlock block, long opCode, int position)
        {
            EmitFfma(block, opCode, ShaderOper.Cr);
        }

        public static void Ffma_I(ShaderIrBlock block, long opCode, int position)
        {
            EmitFfma(block, opCode, ShaderOper.Immf);
        }

        public static void Ffma_RC(ShaderIrBlock block, long opCode, int position)
        {
            EmitFfma(block, opCode, ShaderOper.Rc);
        }

        public static void Ffma_RR(ShaderIrBlock block, long opCode, int position)
        {
            EmitFfma(block, opCode, ShaderOper.Rr);
        }

        public static void Fmnmx_C(ShaderIrBlock block, long opCode, int position)
        {
            EmitFmnmx(block, opCode, ShaderOper.Cr);
        }

        public static void Fmnmx_I(ShaderIrBlock block, long opCode, int position)
        {
            EmitFmnmx(block, opCode, ShaderOper.Immf);
        }

        public static void Fmnmx_R(ShaderIrBlock block, long opCode, int position)
        {
            EmitFmnmx(block, opCode, ShaderOper.Rr);
        }

        public static void Fmul_I32(ShaderIrBlock block, long opCode, int position)
        {
            ShaderIrNode operA = opCode.Gpr8();
            ShaderIrNode operB = opCode.Immf32_20();

            ShaderIrOp op = new ShaderIrOp(ShaderIrInst.Fmul, operA, operB);

            block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), op)));
        }

        public static void Fmul_C(ShaderIrBlock block, long opCode, int position)
        {
            EmitFmul(block, opCode, ShaderOper.Cr);
        }

        public static void Fmul_I(ShaderIrBlock block, long opCode, int position)
        {
            EmitFmul(block, opCode, ShaderOper.Immf);
        }

        public static void Fmul_R(ShaderIrBlock block, long opCode, int position)
        {
            EmitFmul(block, opCode, ShaderOper.Rr);
        }

        public static void Fset_C(ShaderIrBlock block, long opCode, int position)
        {
            EmitFset(block, opCode, ShaderOper.Cr);
        }

        public static void Fset_I(ShaderIrBlock block, long opCode, int position)
        {
            EmitFset(block, opCode, ShaderOper.Immf);
        }

        public static void Fset_R(ShaderIrBlock block, long opCode, int position)
        {
            EmitFset(block, opCode, ShaderOper.Rr);
        }

        public static void Fsetp_C(ShaderIrBlock block, long opCode, int position)
        {
            EmitFsetp(block, opCode, ShaderOper.Cr);
        }

        public static void Fsetp_I(ShaderIrBlock block, long opCode, int position)
        {
            EmitFsetp(block, opCode, ShaderOper.Immf);
        }

        public static void Fsetp_R(ShaderIrBlock block, long opCode, int position)
        {
            EmitFsetp(block, opCode, ShaderOper.Rr);
        }

        public static void Iadd_C(ShaderIrBlock block, long opCode, int position)
        {
            EmitIadd(block, opCode, ShaderOper.Cr);
        }

        public static void Iadd_I(ShaderIrBlock block, long opCode, int position)
        {
            EmitIadd(block, opCode, ShaderOper.Imm);
        }

        public static void Iadd_I32(ShaderIrBlock block, long opCode, int position)
        {
            ShaderIrNode operA = opCode.Gpr8();
            ShaderIrNode operB = opCode.Imm32_20();

            bool negA = opCode.Read(56);

            operA = GetAluIneg(operA, negA);

            ShaderIrOp op = new ShaderIrOp(ShaderIrInst.Add, operA, operB);

            block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), op)));
        }

        public static void Iadd_R(ShaderIrBlock block, long opCode, int position)
        {
            EmitIadd(block, opCode, ShaderOper.Rr);
        }

        public static void Iadd3_C(ShaderIrBlock block, long opCode, int position)
        {
            EmitIadd3(block, opCode, ShaderOper.Cr);
        }

        public static void Iadd3_I(ShaderIrBlock block, long opCode, int position)
        {
            EmitIadd3(block, opCode, ShaderOper.Imm);
        }

        public static void Iadd3_R(ShaderIrBlock block, long opCode, int position)
        {
            EmitIadd3(block, opCode, ShaderOper.Rr);
        }

        public static void Imnmx_C(ShaderIrBlock block, long opCode, int position)
        {
            EmitImnmx(block, opCode, ShaderOper.Cr);
        }

        public static void Imnmx_I(ShaderIrBlock block, long opCode, int position)
        {
            EmitImnmx(block, opCode, ShaderOper.Imm);
        }

        public static void Imnmx_R(ShaderIrBlock block, long opCode, int position)
        {
            EmitImnmx(block, opCode, ShaderOper.Rr);
        }

        public static void Ipa(ShaderIrBlock block, long opCode, int position)
        {
            ShaderIrNode operA = opCode.Abuf28();
            ShaderIrNode operB = opCode.Gpr20();

            ShaderIpaMode mode = (ShaderIpaMode)(opCode.Read(54, 3));

            ShaderIrMetaIpa meta = new ShaderIrMetaIpa(mode);

            ShaderIrOp op = new ShaderIrOp(ShaderIrInst.Ipa, operA, operB, null, meta);

            block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), op)));
        }

        public static void Iscadd_C(ShaderIrBlock block, long opCode, int position)
        {
            EmitIscadd(block, opCode, ShaderOper.Cr);
        }

        public static void Iscadd_I(ShaderIrBlock block, long opCode, int position)
        {
            EmitIscadd(block, opCode, ShaderOper.Imm);
        }

        public static void Iscadd_R(ShaderIrBlock block, long opCode, int position)
        {
            EmitIscadd(block, opCode, ShaderOper.Rr);
        }

        public static void Iset_C(ShaderIrBlock block, long opCode, int position)
        {
            EmitIset(block, opCode, ShaderOper.Cr);
        }

        public static void Iset_I(ShaderIrBlock block, long opCode, int position)
        {
            EmitIset(block, opCode, ShaderOper.Imm);
        }

        public static void Iset_R(ShaderIrBlock block, long opCode, int position)
        {
            EmitIset(block, opCode, ShaderOper.Rr);
        }

        public static void Isetp_C(ShaderIrBlock block, long opCode, int position)
        {
            EmitIsetp(block, opCode, ShaderOper.Cr);
        }

        public static void Isetp_I(ShaderIrBlock block, long opCode, int position)
        {
            EmitIsetp(block, opCode, ShaderOper.Imm);
        }

        public static void Isetp_R(ShaderIrBlock block, long opCode, int position)
        {
            EmitIsetp(block, opCode, ShaderOper.Rr);
        }

        public static void Lop_I32(ShaderIrBlock block, long opCode, int position)
        {
            int subOp = opCode.Read(53, 3);

            bool invA = opCode.Read(55);
            bool invB = opCode.Read(56);

            ShaderIrInst inst = 0;

            switch (subOp)
            {
                case 0: inst = ShaderIrInst.And; break;
                case 1: inst = ShaderIrInst.Or;  break;
                case 2: inst = ShaderIrInst.Xor; break;
            }

            ShaderIrNode operB = GetAluNot(opCode.Imm32_20(), invB);

            //SubOp == 3 is pass, used by the not instruction
            //which just moves the inverted register value.
            if (subOp < 3)
            {
                ShaderIrNode operA = GetAluNot(opCode.Gpr8(), invA);

                ShaderIrOp op = new ShaderIrOp(inst, operA, operB);

                block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), op)));
            }
            else
            {
                block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), operB)));
            }
        }

        public static void Lop_C(ShaderIrBlock block, long opCode, int position)
        {
            EmitLop(block, opCode, ShaderOper.Cr);
        }

        public static void Lop_I(ShaderIrBlock block, long opCode, int position)
        {
            EmitLop(block, opCode, ShaderOper.Imm);
        }

        public static void Lop_R(ShaderIrBlock block, long opCode, int position)
        {
            EmitLop(block, opCode, ShaderOper.Rr);
        }

        public static void Mufu(ShaderIrBlock block, long opCode, int position)
        {
            int subOp = opCode.Read(20, 0xf);

            bool absA = opCode.Read(46);
            bool negA = opCode.Read(48);

            ShaderIrInst inst = 0;

            switch (subOp)
            {
                case 0: inst = ShaderIrInst.Fcos;  break;
                case 1: inst = ShaderIrInst.Fsin;  break;
                case 2: inst = ShaderIrInst.Fex2;  break;
                case 3: inst = ShaderIrInst.Flg2;  break;
                case 4: inst = ShaderIrInst.Frcp;  break;
                case 5: inst = ShaderIrInst.Frsq;  break;
                case 8: inst = ShaderIrInst.Fsqrt; break;

                default: throw new NotImplementedException(subOp.ToString());
            }

            ShaderIrNode operA = opCode.Gpr8();

            ShaderIrOp op = new ShaderIrOp(inst, GetAluFabsFneg(operA, absA, negA));

            block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), op)));
        }

        public static void Psetp(ShaderIrBlock block, long opCode, int position)
        {
            bool negA = opCode.Read(15);
            bool negB = opCode.Read(32);
            bool negP = opCode.Read(42);

            ShaderIrInst lopInst = opCode.BLop24();

            ShaderIrNode operA = opCode.Pred12();
            ShaderIrNode operB = opCode.Pred29();

            if (negA)
            {
                operA = new ShaderIrOp(ShaderIrInst.Bnot, operA);
            }

            if (negB)
            {
                operB = new ShaderIrOp(ShaderIrInst.Bnot, operB);
            }

            ShaderIrOp op = new ShaderIrOp(lopInst, operA, operB);

            ShaderIrOperPred p0Node = opCode.Pred3();
            ShaderIrOperPred p1Node = opCode.Pred0();
            ShaderIrOperPred p2Node = opCode.Pred39();

            block.AddNode(opCode.PredNode(new ShaderIrAsg(p0Node, op)));

            lopInst = opCode.BLop45();

            if (lopInst == ShaderIrInst.Band && p1Node.IsConst && p2Node.IsConst)
            {
                return;
            }

            ShaderIrNode p2NNode = p2Node;

            if (negP)
            {
                p2NNode = new ShaderIrOp(ShaderIrInst.Bnot, p2NNode);
            }

            op = new ShaderIrOp(ShaderIrInst.Bnot, p0Node);

            op = new ShaderIrOp(lopInst, op, p2NNode);

            block.AddNode(opCode.PredNode(new ShaderIrAsg(p1Node, op)));

            op = new ShaderIrOp(lopInst, p0Node, p2NNode);

            block.AddNode(opCode.PredNode(new ShaderIrAsg(p0Node, op)));
        }

        public static void Rro_C(ShaderIrBlock block, long opCode, int position)
        {
            EmitRro(block, opCode, ShaderOper.Cr);
        }

        public static void Rro_I(ShaderIrBlock block, long opCode, int position)
        {
            EmitRro(block, opCode, ShaderOper.Immf);
        }

        public static void Rro_R(ShaderIrBlock block, long opCode, int position)
        {
            EmitRro(block, opCode, ShaderOper.Rr);
        }

        public static void Shl_C(ShaderIrBlock block, long opCode, int position)
        {
            EmitAluBinary(block, opCode, ShaderOper.Cr, ShaderIrInst.Lsl);
        }

        public static void Shl_I(ShaderIrBlock block, long opCode, int position)
        {
            EmitAluBinary(block, opCode, ShaderOper.Imm, ShaderIrInst.Lsl);
        }

        public static void Shl_R(ShaderIrBlock block, long opCode, int position)
        {
            EmitAluBinary(block, opCode, ShaderOper.Rr, ShaderIrInst.Lsl);
        }

        public static void Shr_C(ShaderIrBlock block, long opCode, int position)
        {
            EmitAluBinary(block, opCode, ShaderOper.Cr, GetShrInst(opCode));
        }

        public static void Shr_I(ShaderIrBlock block, long opCode, int position)
        {
            EmitAluBinary(block, opCode, ShaderOper.Imm, GetShrInst(opCode));
        }

        public static void Shr_R(ShaderIrBlock block, long opCode, int position)
        {
            EmitAluBinary(block, opCode, ShaderOper.Rr, GetShrInst(opCode));
        }

        private static ShaderIrInst GetShrInst(long opCode)
        {
            bool signed = opCode.Read(48);

            return signed ? ShaderIrInst.Asr : ShaderIrInst.Lsr;
        }

        public static void Vmad(ShaderIrBlock block, long opCode, int position)
        {
            ShaderIrNode operA = opCode.Gpr8();

            ShaderIrNode operB;

            if (opCode.Read(50))
            {
                operB = opCode.Gpr20();
            }
            else
            {
                operB = opCode.Imm19_20();
            }

            ShaderIrOperGpr operC = opCode.Gpr39();

            ShaderIrNode tmp = new ShaderIrOp(ShaderIrInst.Mul, operA, operB);

            ShaderIrNode final = new ShaderIrOp(ShaderIrInst.Add, tmp, operC);

            int shr = opCode.Read(51, 3);

            if (shr != 0)
            {
                int shift = (shr == 2) ? 15 : 7;

                final = new ShaderIrOp(ShaderIrInst.Lsr, final, new ShaderIrOperImm(shift));
            }

            block.AddNode(new ShaderIrCmnt("Stubbed. Instruction is reduced to a * b + c"));

            block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), final)));
        }

        public static void Xmad_CR(ShaderIrBlock block, long opCode, int position)
        {
            EmitXmad(block, opCode, ShaderOper.Cr);
        }

        public static void Xmad_I(ShaderIrBlock block, long opCode, int position)
        {
            EmitXmad(block, opCode, ShaderOper.Imm);
        }

        public static void Xmad_RC(ShaderIrBlock block, long opCode, int position)
        {
            EmitXmad(block, opCode, ShaderOper.Rc);
        }

        public static void Xmad_RR(ShaderIrBlock block, long opCode, int position)
        {
            EmitXmad(block, opCode, ShaderOper.Rr);
        }

        private static void EmitAluBinary(
            ShaderIrBlock block,
            long          opCode,
            ShaderOper    oper,
            ShaderIrInst  inst)
        {
            ShaderIrNode operA = opCode.Gpr8(), operB;

            switch (oper)
            {
                case ShaderOper.Cr:  operB = opCode.Cbuf34();   break;
                case ShaderOper.Imm: operB = opCode.Imm19_20(); break;
                case ShaderOper.Rr:  operB = opCode.Gpr20();    break;

                default: throw new ArgumentException(nameof(oper));
            }

            ShaderIrNode op = new ShaderIrOp(inst, operA, operB);

            block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), op)));
        }

        private static void EmitBfe(ShaderIrBlock block, long opCode, ShaderOper oper)
        {
            //TODO: Handle the case where position + length
            //is greater than the word size, in this case the sign bit
            //needs to be replicated to fill the remaining space.
            bool negB = opCode.Read(48);
            bool negA = opCode.Read(49);

            ShaderIrNode operA = opCode.Gpr8(), operB;

            switch (oper)
            {
                case ShaderOper.Cr:  operB = opCode.Cbuf34();   break;
                case ShaderOper.Imm: operB = opCode.Imm19_20(); break;
                case ShaderOper.Rr:  operB = opCode.Gpr20();    break;

                default: throw new ArgumentException(nameof(oper));
            }

            ShaderIrNode op;

            bool signed = opCode.Read(48); //?

            if (operB is ShaderIrOperImm posLen)
            {
                int position = (posLen.Value >> 0) & 0xff;
                int length   = (posLen.Value >> 8) & 0xff;

                int lSh = 32 - (position + length);

                ShaderIrInst rightShift = signed
                    ? ShaderIrInst.Asr
                    : ShaderIrInst.Lsr;

                op = new ShaderIrOp(ShaderIrInst.Lsl, operA, new ShaderIrOperImm(lSh));
                op = new ShaderIrOp(rightShift,       op,    new ShaderIrOperImm(lSh + position));
            }
            else
            {
                ShaderIrOperImm shift = new ShaderIrOperImm(8);
                ShaderIrOperImm mask  = new ShaderIrOperImm(0xff);

                ShaderIrNode opPos, opLen;

                opPos = new ShaderIrOp(ShaderIrInst.And, operB, mask);
                opLen = new ShaderIrOp(ShaderIrInst.Lsr, operB, shift);
                opLen = new ShaderIrOp(ShaderIrInst.And, opLen, mask);

                op = new ShaderIrOp(ShaderIrInst.Lsr, operA, opPos);

                op = ExtendTo32(op, signed, opLen);
            }

            block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), op)));
        }

        private static void EmitFadd(ShaderIrBlock block, long opCode, ShaderOper oper)
        {
            bool negB = opCode.Read(45);
            bool absA = opCode.Read(46);
            bool negA = opCode.Read(48);
            bool absB = opCode.Read(49);
            bool sat  = opCode.Read(50);

            ShaderIrNode operA = opCode.Gpr8(), operB;

            operA = GetAluFabsFneg(operA, absA, negA);

            switch (oper)
            {
                case ShaderOper.Cr:   operB = opCode.Cbuf34();    break;
                case ShaderOper.Immf: operB = opCode.Immf19_20(); break;
                case ShaderOper.Rr:   operB = opCode.Gpr20();     break;

                default: throw new ArgumentException(nameof(oper));
            }

            operB = GetAluFabsFneg(operB, absB, negB);

            ShaderIrNode op = new ShaderIrOp(ShaderIrInst.Fadd, operA, operB);

            block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), GetAluFsat(op, sat))));
        }

        private static void EmitFmul(ShaderIrBlock block, long opCode, ShaderOper oper)
        {
            bool negB = opCode.Read(48);
            bool sat  = opCode.Read(50);

            ShaderIrNode operA = opCode.Gpr8(), operB;

            switch (oper)
            {
                case ShaderOper.Cr:   operB = opCode.Cbuf34();    break;
                case ShaderOper.Immf: operB = opCode.Immf19_20(); break;
                case ShaderOper.Rr:   operB = opCode.Gpr20();     break;

                default: throw new ArgumentException(nameof(oper));
            }

            operB = GetAluFneg(operB, negB);

            ShaderIrNode op = new ShaderIrOp(ShaderIrInst.Fmul, operA, operB);

            block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), GetAluFsat(op, sat))));
        }

        private static void EmitFfma(ShaderIrBlock block, long opCode, ShaderOper oper)
        {
            bool negB = opCode.Read(48);
            bool negC = opCode.Read(49);
            bool sat  = opCode.Read(50);

            ShaderIrNode operA = opCode.Gpr8(), operB, operC;

            switch (oper)
            {
                case ShaderOper.Cr:   operB = opCode.Cbuf34();    break;
                case ShaderOper.Immf: operB = opCode.Immf19_20(); break;
                case ShaderOper.Rc:   operB = opCode.Gpr39();     break;
                case ShaderOper.Rr:   operB = opCode.Gpr20();     break;

                default: throw new ArgumentException(nameof(oper));
            }

            operB = GetAluFneg(operB, negB);

            if (oper == ShaderOper.Rc)
            {
                operC = GetAluFneg(opCode.Cbuf34(), negC);
            }
            else
            {
                operC = GetAluFneg(opCode.Gpr39(), negC);
            }

            ShaderIrOp op = new ShaderIrOp(ShaderIrInst.Ffma, operA, operB, operC);

            block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), GetAluFsat(op, sat))));
        }

        private static void EmitIadd(ShaderIrBlock block, long opCode, ShaderOper oper)
        {
            ShaderIrNode operA = opCode.Gpr8();
            ShaderIrNode operB;

            switch (oper)
            {
                case ShaderOper.Cr:  operB = opCode.Cbuf34();   break;
                case ShaderOper.Imm: operB = opCode.Imm19_20(); break;
                case ShaderOper.Rr:  operB = opCode.Gpr20();    break;

                default: throw new ArgumentException(nameof(oper));
            }

            bool negA = opCode.Read(49);
            bool negB = opCode.Read(48);

            operA = GetAluIneg(operA, negA);
            operB = GetAluIneg(operB, negB);

            ShaderIrOp op = new ShaderIrOp(ShaderIrInst.Add, operA, operB);

            block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), op)));
        }

        private static void EmitIadd3(ShaderIrBlock block, long opCode, ShaderOper oper)
        {
            int mode = opCode.Read(37, 3);

            bool neg1 = opCode.Read(51);
            bool neg2 = opCode.Read(50);
            bool neg3 = opCode.Read(49);

            int height1 = opCode.Read(35, 3);
            int height2 = opCode.Read(33, 3);
            int height3 = opCode.Read(31, 3);

            ShaderIrNode operB;

            switch (oper)
            {
                case ShaderOper.Cr:  operB = opCode.Cbuf34();   break;
                case ShaderOper.Imm: operB = opCode.Imm19_20(); break;
                case ShaderOper.Rr:  operB = opCode.Gpr20();    break;

                default: throw new ArgumentException(nameof(oper));
            }

            ShaderIrNode ApplyHeight(ShaderIrNode src, int height)
            {
                if (oper != ShaderOper.Rr)
                {
                    return src;
                }

                switch (height)
                {
                    case 0: return src;
                    case 1: return new ShaderIrOp(ShaderIrInst.And, src, new ShaderIrOperImm(0xffff));
                    case 2: return new ShaderIrOp(ShaderIrInst.Lsr, src, new ShaderIrOperImm(16));

                    default: throw new InvalidOperationException();
                }
            }

            ShaderIrNode src1 = GetAluIneg(ApplyHeight(opCode.Gpr8(),  height1), neg1);
            ShaderIrNode src2 = GetAluIneg(ApplyHeight(operB,                height2), neg2);
            ShaderIrNode src3 = GetAluIneg(ApplyHeight(opCode.Gpr39(), height3), neg3);

            ShaderIrOp sum = new ShaderIrOp(ShaderIrInst.Add, src1, src2);

            if (oper == ShaderOper.Rr)
            {
                switch (mode)
                {
                    case 1: sum = new ShaderIrOp(ShaderIrInst.Lsr, sum, new ShaderIrOperImm(16)); break;
                    case 2: sum = new ShaderIrOp(ShaderIrInst.Lsl, sum, new ShaderIrOperImm(16)); break;
                }
            }

            //Note: Here there should be a "+ 1" when carry flag is set
            //but since carry is mostly ignored by other instructions, it's excluded for now

            block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), new ShaderIrOp(ShaderIrInst.Add, sum, src3))));
        }

        private static void EmitIscadd(ShaderIrBlock block, long opCode, ShaderOper oper)
        {
            bool negB = opCode.Read(48);
            bool negA = opCode.Read(49);

            ShaderIrNode operA = opCode.Gpr8(), operB;

            ShaderIrOperImm scale = opCode.Imm5_39();

            switch (oper)
            {
                case ShaderOper.Cr:  operB = opCode.Cbuf34();   break;
                case ShaderOper.Imm: operB = opCode.Imm19_20(); break;
                case ShaderOper.Rr:  operB = opCode.Gpr20();    break;

                default: throw new ArgumentException(nameof(oper));
            }

            operA = GetAluIneg(operA, negA);
            operB = GetAluIneg(operB, negB);

            ShaderIrOp scaleOp = new ShaderIrOp(ShaderIrInst.Lsl, operA, scale);
            ShaderIrOp addOp   = new ShaderIrOp(ShaderIrInst.Add, operB, scaleOp);

            block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), addOp)));
        }

        private static void EmitFmnmx(ShaderIrBlock block, long opCode, ShaderOper oper)
        {
            EmitMnmx(block, opCode, true, oper);
        }

        private static void EmitImnmx(ShaderIrBlock block, long opCode, ShaderOper oper)
        {
            EmitMnmx(block, opCode, false, oper);
        }

        private static void EmitMnmx(ShaderIrBlock block, long opCode, bool isFloat, ShaderOper oper)
        {
            bool negB = opCode.Read(45);
            bool absA = opCode.Read(46);
            bool negA = opCode.Read(48);
            bool absB = opCode.Read(49);

            ShaderIrNode operA = opCode.Gpr8(), operB;

            if (isFloat)
            {
                operA = GetAluFabsFneg(operA, absA, negA);
            }
            else
            {
                operA = GetAluIabsIneg(operA, absA, negA);
            }

            switch (oper)
            {
                case ShaderOper.Cr:   operB = opCode.Cbuf34();    break;
                case ShaderOper.Imm:  operB = opCode.Imm19_20();  break;
                case ShaderOper.Immf: operB = opCode.Immf19_20(); break;
                case ShaderOper.Rr:   operB = opCode.Gpr20();     break;

                default: throw new ArgumentException(nameof(oper));
            }

            if (isFloat)
            {
                operB = GetAluFabsFneg(operB, absB, negB);
            }
            else
            {
                operB = GetAluIabsIneg(operB, absB, negB);
            }

            ShaderIrOperPred pred = opCode.Pred39();

            ShaderIrOp op;

            ShaderIrInst maxInst = isFloat ? ShaderIrInst.Fmax : ShaderIrInst.Max;
            ShaderIrInst minInst = isFloat ? ShaderIrInst.Fmin : ShaderIrInst.Min;

            if (pred.IsConst)
            {
                bool isMax = opCode.Read(42);

                op = new ShaderIrOp(isMax
                    ? maxInst
                    : minInst, operA, operB);

                block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), op)));
            }
            else
            {
                ShaderIrNode predN = opCode.Pred39N();

                ShaderIrOp opMax = new ShaderIrOp(maxInst, operA, operB);
                ShaderIrOp opMin = new ShaderIrOp(minInst, operA, operB);

                ShaderIrAsg asgMax = new ShaderIrAsg(opCode.Gpr0(), opMax);
                ShaderIrAsg asgMin = new ShaderIrAsg(opCode.Gpr0(), opMin);

                block.AddNode(opCode.PredNode(new ShaderIrCond(predN, asgMax, not: true)));
                block.AddNode(opCode.PredNode(new ShaderIrCond(predN, asgMin, not: false)));
            }
        }

        private static void EmitRro(ShaderIrBlock block, long opCode, ShaderOper oper)
        {
            //Note: this is a range reduction instruction and is supposed to
            //be used with Mufu, here it just moves the value and ignores the operation.
            bool negA = opCode.Read(45);
            bool absA = opCode.Read(49);

            ShaderIrNode operA;

            switch (oper)
            {
                case ShaderOper.Cr:   operA = opCode.Cbuf34();    break;
                case ShaderOper.Immf: operA = opCode.Immf19_20(); break;
                case ShaderOper.Rr:   operA = opCode.Gpr20();     break;

                default: throw new ArgumentException(nameof(oper));
            }

            operA = GetAluFabsFneg(operA, absA, negA);

            block.AddNode(new ShaderIrCmnt("Stubbed."));

            block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), operA)));
        }

        private static void EmitFset(ShaderIrBlock block, long opCode, ShaderOper oper)
        {
            EmitSet(block, opCode, true, oper);
        }

        private static void EmitIset(ShaderIrBlock block, long opCode, ShaderOper oper)
        {
            EmitSet(block, opCode, false, oper);
        }

        private static void EmitSet(ShaderIrBlock block, long opCode, bool isFloat, ShaderOper oper)
        {
            bool negA = opCode.Read(43);
            bool absB = opCode.Read(44);
            bool negB = opCode.Read(53);
            bool absA = opCode.Read(54);

            bool boolFloat = opCode.Read(isFloat ? 52 : 44);

            ShaderIrNode operA = opCode.Gpr8(), operB;

            switch (oper)
            {
                case ShaderOper.Cr:   operB = opCode.Cbuf34();    break;
                case ShaderOper.Imm:  operB = opCode.Imm19_20();  break;
                case ShaderOper.Immf: operB = opCode.Immf19_20(); break;
                case ShaderOper.Rr:   operB = opCode.Gpr20();     break;

                default: throw new ArgumentException(nameof(oper));
            }

            ShaderIrInst cmpInst;

            if (isFloat)
            {
                operA = GetAluFabsFneg(operA, absA, negA);
                operB = GetAluFabsFneg(operB, absB, negB);

                cmpInst = opCode.CmpF();
            }
            else
            {
                cmpInst = opCode.Cmp();
            }

            ShaderIrOp op = new ShaderIrOp(cmpInst, operA, operB);

            ShaderIrInst lopInst = opCode.BLop45();

            ShaderIrOperPred pNode = opCode.Pred39();

            ShaderIrNode imm0, imm1;

            if (boolFloat)
            {
                imm0 = new ShaderIrOperImmf(0);
                imm1 = new ShaderIrOperImmf(1);
            }
            else
            {
                imm0 = new ShaderIrOperImm(0);
                imm1 = new ShaderIrOperImm(-1);
            }

            ShaderIrNode asg0 = new ShaderIrAsg(opCode.Gpr0(), imm0);
            ShaderIrNode asg1 = new ShaderIrAsg(opCode.Gpr0(), imm1);

            if (lopInst != ShaderIrInst.Band || !pNode.IsConst)
            {
                ShaderIrOp op2 = new ShaderIrOp(lopInst, op, pNode);

                asg0 = new ShaderIrCond(op2, asg0, not: true);
                asg1 = new ShaderIrCond(op2, asg1, not: false);
            }
            else
            {
                asg0 = new ShaderIrCond(op, asg0, not: true);
                asg1 = new ShaderIrCond(op, asg1, not: false);
            }

            block.AddNode(opCode.PredNode(asg0));
            block.AddNode(opCode.PredNode(asg1));
        }

        private static void EmitFsetp(ShaderIrBlock block, long opCode, ShaderOper oper)
        {
            EmitSetp(block, opCode, true, oper);
        }

        private static void EmitIsetp(ShaderIrBlock block, long opCode, ShaderOper oper)
        {
            EmitSetp(block, opCode, false, oper);
        }

        private static void EmitSetp(ShaderIrBlock block, long opCode, bool isFloat, ShaderOper oper)
        {
            bool absA = opCode.Read(7);
            bool negP = opCode.Read(42);
            bool negA = opCode.Read(43);
            bool absB = opCode.Read(44);

            ShaderIrNode operA = opCode.Gpr8(), operB;

            switch (oper)
            {
                case ShaderOper.Cr:   operB = opCode.Cbuf34();    break;
                case ShaderOper.Imm:  operB = opCode.Imm19_20();  break;
                case ShaderOper.Immf: operB = opCode.Immf19_20(); break;
                case ShaderOper.Rr:   operB = opCode.Gpr20();     break;

                default: throw new ArgumentException(nameof(oper));
            }

            ShaderIrInst cmpInst;

            if (isFloat)
            {
                operA = GetAluFabsFneg(operA, absA, negA);
                operB = GetAluFabs    (operB, absB);

                cmpInst = opCode.CmpF();
            }
            else
            {
                cmpInst = opCode.Cmp();
            }

            ShaderIrOp op = new ShaderIrOp(cmpInst, operA, operB);

            ShaderIrOperPred p0Node = opCode.Pred3();
            ShaderIrOperPred p1Node = opCode.Pred0();
            ShaderIrOperPred p2Node = opCode.Pred39();

            block.AddNode(opCode.PredNode(new ShaderIrAsg(p0Node, op)));

            ShaderIrInst lopInst = opCode.BLop45();

            if (lopInst == ShaderIrInst.Band && p1Node.IsConst && p2Node.IsConst)
            {
                return;
            }

            ShaderIrNode p2NNode = p2Node;

            if (negP)
            {
                p2NNode = new ShaderIrOp(ShaderIrInst.Bnot, p2NNode);
            }

            op = new ShaderIrOp(ShaderIrInst.Bnot, p0Node);

            op = new ShaderIrOp(lopInst, op, p2NNode);

            block.AddNode(opCode.PredNode(new ShaderIrAsg(p1Node, op)));

            op = new ShaderIrOp(lopInst, p0Node, p2NNode);

            block.AddNode(opCode.PredNode(new ShaderIrAsg(p0Node, op)));
        }

        private static void EmitLop(ShaderIrBlock block, long opCode, ShaderOper oper)
        {
            int subOp = opCode.Read(41, 3);

            bool invA = opCode.Read(39);
            bool invB = opCode.Read(40);

            ShaderIrInst inst = 0;

            switch (subOp)
            {
                case 0: inst = ShaderIrInst.And; break;
                case 1: inst = ShaderIrInst.Or;  break;
                case 2: inst = ShaderIrInst.Xor; break;
            }

            ShaderIrNode operA = GetAluNot(opCode.Gpr8(), invA);
            ShaderIrNode operB;

            switch (oper)
            {
                case ShaderOper.Cr:  operB = opCode.Cbuf34();   break;
                case ShaderOper.Imm: operB = opCode.Imm19_20(); break;
                case ShaderOper.Rr:  operB = opCode.Gpr20();    break;

                default: throw new ArgumentException(nameof(oper));
            }

            operB = GetAluNot(operB, invB);

            ShaderIrNode op;

            if (subOp < 3)
            {
                op = new ShaderIrOp(inst, operA, operB);
            }
            else
            {
                op = operB;
            }

            ShaderIrNode compare = new ShaderIrOp(ShaderIrInst.Cne, op, new ShaderIrOperImm(0));

            block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Pred48(), compare)));

            block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), op)));
        }

        private static void EmitXmad(ShaderIrBlock block, long opCode, ShaderOper oper)
        {
            //TODO: Confirm SignAB/C, it is just a guess.
            //TODO: Implement Mode 3 (CSFU), what it does?
            bool signAb = opCode.Read(48);
            bool signC  = opCode.Read(49);
            bool highB  = opCode.Read(52);
            bool highA  = opCode.Read(53);

            int mode = opCode.Read(50, 7);

            ShaderIrNode operA = opCode.Gpr8(), operB, operC;

            ShaderIrOperImm imm16  = new ShaderIrOperImm(16);
            ShaderIrOperImm immMsk = new ShaderIrOperImm(0xffff);

            ShaderIrInst shiftAb = signAb ? ShaderIrInst.Asr : ShaderIrInst.Lsr;
            ShaderIrInst shiftC  = signC  ? ShaderIrInst.Asr : ShaderIrInst.Lsr;

            if (highA)
            {
                operA = new ShaderIrOp(shiftAb, operA, imm16);
            }

            switch (oper)
            {
                case ShaderOper.Cr:  operB = opCode.Cbuf34();   break;
                case ShaderOper.Imm: operB = opCode.Imm19_20(); break;
                case ShaderOper.Rc:  operB = opCode.Gpr39();    break;
                case ShaderOper.Rr:  operB = opCode.Gpr20();    break;

                default: throw new ArgumentException(nameof(oper));
            }

            bool productShiftLeft = false, merge = false;

            if (oper == ShaderOper.Rc)
            {
                operC = opCode.Cbuf34();
            }
            else
            {
                operC = opCode.Gpr39();

                productShiftLeft = opCode.Read(36);
                merge            = opCode.Read(37);
            }

            switch (mode)
            {
                //CLO.
                case 1: operC = ExtendTo32(operC, signC, 16); break;

                //CHI.
                case 2: operC = new ShaderIrOp(shiftC, operC, imm16); break;
            }

            ShaderIrNode operBh = operB;

            if (highB)
            {
                operBh = new ShaderIrOp(shiftAb, operBh, imm16);
            }

            ShaderIrOp mulOp = new ShaderIrOp(ShaderIrInst.Mul, operA, operBh);

            if (productShiftLeft)
            {
                mulOp = new ShaderIrOp(ShaderIrInst.Lsl, mulOp, imm16);
            }

            ShaderIrOp addOp = new ShaderIrOp(ShaderIrInst.Add, mulOp, operC);

            if (merge)
            {
                addOp = new ShaderIrOp(ShaderIrInst.And, addOp, immMsk);
                operB = new ShaderIrOp(ShaderIrInst.Lsl, operB, imm16);
                addOp = new ShaderIrOp(ShaderIrInst.Or,  addOp, operB);
            }

            if (mode == 4)
            {
                operB = new ShaderIrOp(ShaderIrInst.Lsl, operB, imm16);
                addOp = new ShaderIrOp(ShaderIrInst.Or,  addOp, operB);
            }

            block.AddNode(opCode.PredNode(new ShaderIrAsg(opCode.Gpr0(), addOp)));
        }
    }
}