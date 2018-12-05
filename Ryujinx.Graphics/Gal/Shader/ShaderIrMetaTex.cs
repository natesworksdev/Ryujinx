using Ryujinx.Graphics.Texture;

namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrMetaTex : ShaderIrMeta
    {
        public int                      Elem { get; private set; }
        public GalTextureTarget              TextureType { get; private set; }
        public ShaderIrNode[]           Coordinates { get; private set; }
        public TextureInstructionSuffix TextureInstructionSuffix { get; private set; }
        public ShaderIrOperGpr          LevelOfDetail;
        public ShaderIrOperGpr          Offset;
        public ShaderIrOperGpr          DepthCompare;
        public int                      Component; // for TLD4(S)

        public ShaderIrMetaTex(int Elem, GalTextureTarget TextureType, TextureInstructionSuffix TextureInstructionSuffix, params ShaderIrNode[] Coordinates)
        {
            this.Elem                     = Elem;
            this.TextureType              = TextureType;
            this.TextureInstructionSuffix = TextureInstructionSuffix;
            this.Coordinates              = Coordinates;
        }
    }
}