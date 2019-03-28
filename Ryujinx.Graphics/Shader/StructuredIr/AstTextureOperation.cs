using Ryujinx.Graphics.Shader.IntermediateRepresentation;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    class AstTextureOperation : AstOperation
    {
        public TextureType Type { get; }

        public int   TextureHandle { get; }
        public int[] Components    { get; }

        public AstTextureOperation(
            Instruction       inst,
            TextureType       type,
            int               textureHandle,
            int[]             components,
            params IAstNode[] sources) : base(inst, sources)
        {
            Type          = type;
            TextureHandle = textureHandle;
            Components    = components;
        }
    }
}