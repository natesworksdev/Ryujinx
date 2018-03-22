using System;

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

            EmitAluOperANode(Block, OpCode);

            if (Inst == ShaderIrInst.Fadd)
            {
                EmitAluAbsNeg(Block, Aa, Na);
            }

            switch (Oper)
            {
                case ShaderOper.RR:  EmitAluOperBNode_RR (Block, OpCode); break;
                case ShaderOper.CR:  EmitAluOperBCNode_C (Block, OpCode); break;
                case ShaderOper.Imm: EmitAluOperBNode_Imm(Block, OpCode); break;

                default: throw new ArgumentException(nameof(Oper));
            }

            EmitAluAbsNeg(Block, Ab, Nb);

            Block.AddNode(new ShaderIrNode(Inst));

            EmitAluAbs(Block, Ad);

            EmitAluStrResult(Block, OpCode);
        }

        private static void EmitAluFfma(ShaderIrBlock Block, long OpCode, ShaderOper Oper)
        {
            bool Nb = ((OpCode >> 48) & 1) != 0;
            bool Nc = ((OpCode >> 49) & 1) != 0;

            EmitAluOperANode(Block, OpCode);

            switch (Oper)
            {
                case ShaderOper.RR:  EmitAluOperBNode_RR (Block, OpCode); break;
                case ShaderOper.CR:  EmitAluOperBCNode_C (Block, OpCode); break;
                case ShaderOper.RC:  EmitAluOperBCNode_R (Block, OpCode); break;
                case ShaderOper.Imm: EmitAluOperBNode_Imm(Block, OpCode); break;
            }

            EmitAluNeg(Block, Nb);

            Block.AddNode(new ShaderIrNode(ShaderIrInst.Fmul));

            if (Oper == ShaderOper.RC)
            {
                EmitAluOperBCNode_C(Block, OpCode);
            }
            else
            {
                EmitAluOperBCNode_R(Block, OpCode);
            }

            EmitAluNeg(Block, Nc);

            Block.AddNode(new ShaderIrNode(ShaderIrInst.Fadd));

            EmitAluStrResult(Block, OpCode);
        }

        private static void EmitAluAbsNeg(ShaderIrBlock Block, bool Abs, bool Neg)
        {
            EmitAluAbs(Block, Abs);
            EmitAluNeg(Block, Neg);
        }

        private static void EmitAluAbs(ShaderIrBlock Block, bool Abs)
        {
            if (Abs)
            {
                Block.AddNode(new ShaderIrNode(ShaderIrInst.Fabs));
            }
        }

        private static void EmitAluNeg(ShaderIrBlock Block, bool Neg)
        {
            if (Neg)
            {
                Block.AddNode(new ShaderIrNode(ShaderIrInst.Fneg));
            }
        }

        private static void EmitAluOperANode(ShaderIrBlock Block, long OpCode)
        {
            Block.AddNode(new ShaderIrNodeLdr((int)(OpCode >> 8) & 0xff));
        }

        private static void EmitAluOperBNode_RR(ShaderIrBlock Block, long OpCode)
        {
            Block.AddNode(new ShaderIrNodeLdr((int)(OpCode >> 20) & 0xff));
        }

        private static void EmitAluOperBCNode_R(ShaderIrBlock Block, long OpCode)
        {
            Block.AddNode(new ShaderIrNodeLdr((int)(OpCode >> 39) & 0xff));
        }

        private static void EmitAluOperBCNode_C(ShaderIrBlock Block, long OpCode)
        {
            Block.AddNode(new ShaderIrNodeLdb(
                (int)(OpCode >> 34) & 0x1f,
                (int)(OpCode >> 20) & 0x3fff));
        }

        private static void EmitAluOperBNode_Imm(ShaderIrBlock Block, long OpCode)
        {
            //TODO
        }

        private static void EmitAluStrResult(ShaderIrBlock Block, long OpCode)
        {
            Block.AddNode(new ShaderIrNodeStr((int)(OpCode >>  0) & 0xff));
        }
    }
}