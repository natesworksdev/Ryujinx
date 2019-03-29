using Ryujinx.Graphics.Shader.IntermediateRepresentation;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    class AstTextureOperation : AstOperation
    {
        public TextureType  Type  { get; }
        public TextureFlags Flags { get; }

        public int   TextureHandle { get; }
        public int[] Components    { get; }

        public AstTextureOperation(
            Instruction       inst,
            TextureType       type,
            TextureFlags      flags,
            int               textureHandle,
            int[]             components,
            params IAstNode[] sources) : base(inst, sources)
        {
            Type          = type;
            Flags         = flags;
            TextureHandle = textureHandle;
            Components    = components;
        }
    }
}