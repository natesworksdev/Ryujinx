using Ryujinx.Graphics.Texture;

namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrMetaTex : ShaderIrMeta
    {
        public int Elem { get; private set; }
        public TextureType TextureType { get; private set; }
        public ShaderIrNode[] Coordinates { get; private set; }

        public ShaderIrMetaTex(int Elem, TextureType TextureType, params ShaderIrNode[] Coordinates)
        {
            this.Elem        = Elem;
            this.TextureType = TextureType;
            this.Coordinates = Coordinates;
        }
    }
}