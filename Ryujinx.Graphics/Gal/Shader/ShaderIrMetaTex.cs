using Ryujinx.Graphics.Texture;

namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrMetaTex : ShaderIrMeta
    {
        public int                      Elem { get; private set; }
        public TextureType              TextureType { get; private set; }
        public ShaderIrNode[]           Coordinates { get; private set; }
        public TextureInstructionSuffix TextureInstructionSuffix { get; private set; }
        public ShaderIrOperGpr          LevelOfDetail;
        public ShaderIrOperGpr          Offset;
        public ShaderIrOperGpr          DepthCompare;

        public ShaderIrMetaTex(int Elem, TextureType TextureType, TextureInstructionSuffix TextureInstructionSuffix, params ShaderIrNode[] Coordinates)
        {
            this.Elem                     = Elem;
            this.TextureType              = TextureType;
            this.TextureInstructionSuffix = TextureInstructionSuffix;
            this.Coordinates              = Coordinates;
        }
    }
}