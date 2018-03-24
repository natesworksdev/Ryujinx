using static Ryujinx.Graphics.Gal.Shader.ShaderDecodeHelper;

namespace Ryujinx.Graphics.Gal.Shader
{
    static partial class ShaderDecode
    {
        public static void Ld_A(ShaderIrBlock Block, long OpCode)
        {
            ShaderIrOper[] Opers = GetAluOperANode_A(OpCode);

            int Index = 0;

            foreach (ShaderIrOper OperA in Opers)
            {
                ShaderIrOperReg OperD = GetAluOperDNode(OpCode);

                OperD.GprIndex += Index++;

                Block.AddNode(new ShaderIrNode(OperD, OperA));
            }
        }

        public static void St_A(ShaderIrBlock Block, long OpCode)
        {
            ShaderIrOper[] Opers = GetAluOperANode_A(OpCode);

            int Index = 0;

            foreach (ShaderIrOper OperA in Opers)
            {
                ShaderIrOperReg OperD = GetAluOperDNode(OpCode);

                OperD.GprIndex += Index++;

                Block.AddNode(new ShaderIrNode(OperA, OperD));
            }
        }
    }
}