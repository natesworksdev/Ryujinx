namespace Ryujinx.Graphics.Gal.Shader
{
    static class ShaderDecoder
    {
        public static ShaderIrBlock DecodeBasicBlock(int[] Code, int Offset, ShaderType Type)
        {
            ShaderIrBlock Block = new ShaderIrBlock();

            while (Offset + 2 <= Code.Length)
            {
                uint Word0 = (uint)Code[Offset++];
                uint Word1 = (uint)Code[Offset++];

                long OpCode = Word0 | (long)Word1 << 32;                

                ShaderDecodeFunc Decode = ShaderOpCodeTable.GetDecoder(OpCode);

                if (Decode == null)
                {
                    continue;
                }

                Decode(Block, OpCode);
            }

            if (Type == ShaderType.Fragment)
            {
                Block.AddNode(new ShaderIrAsg(new ShaderIrOperAbuf(0x70, 0), new ShaderIrOperGpr(0)));
                Block.AddNode(new ShaderIrAsg(new ShaderIrOperAbuf(0x74, 0), new ShaderIrOperGpr(1)));
                Block.AddNode(new ShaderIrAsg(new ShaderIrOperAbuf(0x78, 0), new ShaderIrOperGpr(2)));
                Block.AddNode(new ShaderIrAsg(new ShaderIrOperAbuf(0x7c, 0), new ShaderIrOperGpr(3)));
            }

            Block.RunOptimizationPasses();

            return Block;
        }
    }
}