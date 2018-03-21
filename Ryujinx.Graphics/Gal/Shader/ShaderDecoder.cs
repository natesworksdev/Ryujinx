namespace Ryujinx.Graphics.Gal.Shader
{
    static class ShaderDecoder
    {
        public static ShaderIrBlock DecodeBasicBlock(int[] Code, int Offset)
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

            return Block;
        }
    }
}