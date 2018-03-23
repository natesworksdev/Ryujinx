using static Ryujinx.Graphics.Gal.Shader.ShaderDecodeHelper;

namespace Ryujinx.Graphics.Gal.Shader
{
    static partial class ShaderDecode
    {
        public static void Ld_A(ShaderIrBlock Block, long OpCode)
        {
            ShaderIrOper[] Opers = GetAluOperANode_A(OpCode);
            ShaderIrOper   OperD = GetAluOperDNode(OpCode);

            foreach (ShaderIrOper OperA in Opers)
            {
                Block.AddNode(new ShaderIrNode(OperD, OperA));
            }
        }

        public static void St_A(ShaderIrBlock Block, long OpCode)
        {
            ShaderIrOper[] Opers = GetAluOperANode_A(OpCode);
            ShaderIrOper   OperD = GetAluOperDNode(OpCode);

            foreach (ShaderIrOper OperA in Opers)
            {
                Block.AddNode(new ShaderIrNode(OperA, OperD));
            }
        }
    }
}