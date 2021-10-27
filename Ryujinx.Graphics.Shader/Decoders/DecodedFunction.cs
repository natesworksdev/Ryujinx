using Ryujinx.Graphics.Shader.Translation;

namespace Ryujinx.Graphics.Shader.Decoders
{
    class DecodedFunction
    {
        public bool IsCompilerGenerated => MatchName != FunctionMatchResult.NoMatch;
        public FunctionMatchResult MatchName { get; set; }
        public int Id { get; set; }

        public ulong Address => Blocks[0].Address;
        public Block[] Blocks { get; }

        public DecodedFunction(Block[] blocks)
        {
            MatchName = FunctionMatchResult.NoMatch;
            Id = -1;
            Blocks = blocks;
        }
    }
}