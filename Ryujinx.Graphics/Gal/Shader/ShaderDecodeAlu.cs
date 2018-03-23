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

            ShaderIrOper OperA = GetAluOperANode_R(OpCode);
            ShaderIrOper OperB;

            if (Inst == ShaderIrInst.Fadd)
            {
                OperA = GetAluAbsNeg(OperA, Aa, Na);
            }

            switch (Oper)
            {
                case ShaderOper.RR:  OperB = GetAluOperBNode_RR (OpCode); break;
                case ShaderOper.CR:  OperB = GetAluOperBCNode_C (OpCode); break;
                case ShaderOper.Imm: OperB = GetAluOperBNode_Imm(OpCode); break;

                default: throw new ArgumentException(nameof(Oper));
            }

            OperB = GetAluAbsNeg(OperB, Ab, Nb);

            ShaderIrOper Op = GetAluAbs(new ShaderIrOperOp(Inst, OperA, OperB), Ad);

            Block.AddNode(new ShaderIrNode(GetAluOperDNode(OpCode), Op));
        }

        private static void EmitAluFfma(ShaderIrBlock Block, long OpCode, ShaderOper Oper)
        {
            bool Nb = ((OpCode >> 48) & 1) != 0;
            bool Nc = ((OpCode >> 49) & 1) != 0;

            ShaderIrOper OperA = GetAluOperANode_R(OpCode);
            ShaderIrOper OperB;
            ShaderIrOper OperC;

            switch (Oper)
            {
                case ShaderOper.RR:  OperB = GetAluOperBNode_RR (OpCode); break;
                case ShaderOper.CR:  OperB = GetAluOperBCNode_C (OpCode); break;
                case ShaderOper.RC:  OperB = GetAluOperBCNode_R (OpCode); break;
                case ShaderOper.Imm: OperB = GetAluOperBNode_Imm(OpCode); break;

                default: throw new ArgumentException(nameof(Oper));
            }

            OperB = GetAluNeg(OperB, Nb);

            if (Oper == ShaderOper.RC)
            {
                OperC = GetAluNeg(GetAluOperBCNode_C(OpCode), Nc);
            }
            else
            {
                OperC = GetAluNeg(GetAluOperBCNode_R(OpCode), Nc);
            }

            ShaderIrOper Op = new ShaderIrOperOp(ShaderIrInst.Ffma, OperA, OperB, OperC);

            Block.AddNode(new ShaderIrNode(GetAluOperDNode(OpCode), Op));
        }

        private static ShaderIrOper GetAluAbsNeg(ShaderIrOper Node, bool Abs, bool Neg)
        {
            return GetAluNeg(GetAluAbs(Node, Abs), Neg);
        }

        private static ShaderIrOper GetAluAbs(ShaderIrOper Node, bool Abs)
        {
            return Abs ? new ShaderIrOperOp(ShaderIrInst.Fabs, Node) : Node;
        }

        private static ShaderIrOper GetAluNeg(ShaderIrOper Node, bool Neg)
        {
            return Neg ? new ShaderIrOperOp(ShaderIrInst.Fneg, Node) : Node;
        }
    }
}