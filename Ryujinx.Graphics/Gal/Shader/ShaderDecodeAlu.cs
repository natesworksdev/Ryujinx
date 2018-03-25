using System;

using static Ryujinx.Graphics.Gal.Shader.ShaderDecodeHelper;

namespace Ryujinx.Graphics.Gal.Shader
{
    static partial class ShaderDecode
    {
        private enum ShaderOper
        {
            RR,
            RC,
            CR,
            Imm
        }

        public static void Fadd_R(ShaderIrBlock Block, long OpCode)
        {
            EmitAluBinary(Block, OpCode, ShaderOper.RR, ShaderIrInst.Fadd);
        }

        public static void Fadd_C(ShaderIrBlock Block, long OpCode)
        {
            EmitAluBinary(Block, OpCode, ShaderOper.CR, ShaderIrInst.Fadd);
        }

        public static void Fadd_Imm(ShaderIrBlock Block, long OpCode)
        {
            EmitAluBinary(Block, OpCode, ShaderOper.Imm, ShaderIrInst.Fadd);
        }

        public static void Ffma_RR(ShaderIrBlock Block, long OpCode)
        {
            EmitAluFfma(Block, OpCode, ShaderOper.RR);
        }

        public static void Ffma_CR(ShaderIrBlock Block, long OpCode)
        {
            EmitAluFfma(Block, OpCode, ShaderOper.CR);
        }

        public static void Ffma_RC(ShaderIrBlock Block, long OpCode)
        {
            EmitAluFfma(Block, OpCode, ShaderOper.RC);
        }

        public static void Ffma_Imm(ShaderIrBlock Block, long OpCode)
        {
            EmitAluFfma(Block, OpCode, ShaderOper.Imm);
        }

        public static void Fmul_R(ShaderIrBlock Block, long OpCode)
        {
            EmitAluBinary(Block, OpCode, ShaderOper.RR, ShaderIrInst.Fmul);
        }

        public static void Fmul_C(ShaderIrBlock Block, long OpCode)
        {
            EmitAluBinary(Block, OpCode, ShaderOper.CR, ShaderIrInst.Fmul);
        }

        public static void Fmul_Imm(ShaderIrBlock Block, long OpCode)
        {
            EmitAluBinary(Block, OpCode, ShaderOper.Imm, ShaderIrInst.Fmul);
        }

        public static void Fsetp_C(ShaderIrBlock Block, long OpCode)
        {
            bool Aa = ((OpCode >>  7) & 1) != 0;
            bool Na = ((OpCode >> 43) & 1) != 0;
            bool Ab = ((OpCode >> 44) & 1) != 0;

            ShaderIrNode OperA = GetOperGpr8  (OpCode);
            ShaderIrNode OperB = GetOperCbuf34(OpCode);

            ShaderIrInst CmpInst = GetCmp(OpCode);

            ShaderIrOp Op = new ShaderIrOp(CmpInst,
                GetAluAbsNeg(OperA, Aa, Na),
                GetAluAbs   (OperB, Ab));

            ShaderIrOperPred P0Node = GetOperPred3 (OpCode);
            ShaderIrOperPred P1Node = GetOperPred0 (OpCode);
            ShaderIrOperPred P2Node = GetOperPred39(OpCode);

            Block.AddNode(GetPredNode(new ShaderIrAsg(P0Node, Op), OpCode));

            ShaderIrInst LopInst = GetBLop(OpCode);

            if (LopInst      == ShaderIrInst.Band &&
                P1Node.Index == ShaderIrOperPred.UnusedIndex &&
                P2Node.Index == ShaderIrOperPred.UnusedIndex)
            {
                return;
            }

            ShaderIrNode P2NNode = GetOperPred39N(OpCode);

            Op = new ShaderIrOp(LopInst, new ShaderIrOp(ShaderIrInst.Bnot, P0Node), P2NNode);

            Block.AddNode(GetPredNode(new ShaderIrAsg(P1Node, Op), OpCode));

            Op = new ShaderIrOp(LopInst, P0Node, P2NNode);

            Block.AddNode(GetPredNode(new ShaderIrAsg(P0Node, Op), OpCode));
        }

        public static void Ipa(ShaderIrBlock Block, long OpCode)
        {
            ShaderIrNode OperA = GetOperAbuf28(OpCode);
            ShaderIrNode OperB = GetOperGpr20 (OpCode);

            ShaderIrOp Op = new ShaderIrOp(ShaderIrInst.Ipa, OperA, OperB);

            Block.AddNode(GetPredNode(new ShaderIrAsg(GetOperGpr0(OpCode), Op), OpCode));
        }

        public static void Mufu(ShaderIrBlock Block, long OpCode)
        {
            int SubOp = (int)(OpCode >> 20) & 7;

            bool Aa = ((OpCode >> 46) & 1) != 0;
            bool Na = ((OpCode >> 48) & 1) != 0;

            ShaderIrInst Inst = 0;

            switch (SubOp)
            {
                case 0: Inst = ShaderIrInst.Fcos; break;
                case 1: Inst = ShaderIrInst.Fsin; break;
                case 2: Inst = ShaderIrInst.Fex2; break;
                case 3: Inst = ShaderIrInst.Flg2; break;
                case 4: Inst = ShaderIrInst.Frcp; break;
                case 5: Inst = ShaderIrInst.Frsq; break;
            }

            ShaderIrNode OperA = GetOperGpr8(OpCode);

            ShaderIrOp Op = new ShaderIrOp(Inst, GetAluAbsNeg(OperA, Aa, Na));

            Block.AddNode(GetPredNode(new ShaderIrAsg(GetOperGpr0(OpCode), Op), OpCode));
        }

        private static void EmitAluBinary(
            ShaderIrBlock Block,
            long          OpCode,
            ShaderOper    Oper,
            ShaderIrInst  Inst)
        {
            bool Nb = ((OpCode >> 45) & 1) != 0;
            bool Aa = ((OpCode >> 46) & 1) != 0;
            bool Na = ((OpCode >> 48) & 1) != 0;
            bool Ab = ((OpCode >> 49) & 1) != 0;
            bool Ad = ((OpCode >> 50) & 1) != 0;

            ShaderIrNode OperA = GetOperGpr8(OpCode), OperB;

            if (Inst == ShaderIrInst.Fadd)
            {
                OperA = GetAluAbsNeg(OperA, Aa, Na);
            }

            switch (Oper)
            {
                case ShaderOper.RR:  OperB = GetOperGpr20    (OpCode); break;
                case ShaderOper.CR:  OperB = GetOperCbuf34   (OpCode); break;
                case ShaderOper.Imm: OperB = GetOperImmf19_20(OpCode); break;

                default: throw new ArgumentException(nameof(Oper));
            }

            OperB = GetAluAbsNeg(OperB, Ab, Nb);

            ShaderIrNode Op = new ShaderIrOp(Inst, OperA, OperB);

            Op = GetAluAbs(Op, Ad);

            Block.AddNode(GetPredNode(new ShaderIrAsg(GetOperGpr0(OpCode), Op), OpCode));
        }

        private static void EmitAluFfma(ShaderIrBlock Block, long OpCode, ShaderOper Oper)
        {
            bool Nb = ((OpCode >> 48) & 1) != 0;
            bool Nc = ((OpCode >> 49) & 1) != 0;

            ShaderIrNode OperA = GetOperGpr8(OpCode), OperB, OperC;

            switch (Oper)
            {
                case ShaderOper.RR:  OperB = GetOperGpr20    (OpCode); break;
                case ShaderOper.CR:  OperB = GetOperCbuf34   (OpCode); break;
                case ShaderOper.RC:  OperB = GetOperGpr39    (OpCode); break;
                case ShaderOper.Imm: OperB = GetOperImmf19_20(OpCode); break;

                default: throw new ArgumentException(nameof(Oper));
            }

            OperB = GetAluNeg(OperB, Nb);

            if (Oper == ShaderOper.RC)
            {
                OperC = GetAluNeg(GetOperCbuf34(OpCode), Nc);
            }
            else
            {
                OperC = GetAluNeg(GetOperGpr39(OpCode), Nc);
            }

            ShaderIrOp Op = new ShaderIrOp(ShaderIrInst.Ffma, OperA, OperB, OperC);

            Block.AddNode(GetPredNode(new ShaderIrAsg(GetOperGpr0(OpCode), Op), OpCode));
        }

        private static ShaderIrNode GetAluAbsNeg(ShaderIrNode Node, bool Abs, bool Neg)
        {
            return GetAluNeg(GetAluAbs(Node, Abs), Neg);
        }

        private static ShaderIrNode GetAluAbs(ShaderIrNode Node, bool Abs)
        {
            return Abs ? new ShaderIrOp(ShaderIrInst.Fabs, Node) : Node;
        }

        private static ShaderIrNode GetAluNeg(ShaderIrNode Node, bool Neg)
        {
            return Neg ? new ShaderIrOp(ShaderIrInst.Fneg, Node) : Node;
        }
    }
}