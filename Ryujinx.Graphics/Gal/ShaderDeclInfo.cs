using Ryujinx.Graphics.Texture;

namespace Ryujinx.Graphics.Gal
{
    public class ShaderDeclInfo
    {
        public string Name { get; private set; }

        public int  Index { get; private set; }
        public bool IsCb  { get; private set; }
        public int  Cbuf  { get; private set; }
        public int  Size  { get; private set; }

        public TextureType TextureType { get; private set; }

        public TextureInstructionSuffix TextureSuffix { get; private set; }

        public ShaderDeclInfo(
            string Name,
            int    Index,
            bool   IsCb = false,
            int    Cbuf = 0,
            int    Size = 1,
            TextureType TextureType = TextureType.TwoD,
            TextureInstructionSuffix TextureSuffix = TextureInstructionSuffix.None)
        {
            this.Name        = Name;
            this.Index       = Index;
            this.IsCb        = IsCb;
            this.Cbuf        = Cbuf;
            this.Size        = Size;
            this.TextureType = TextureType;

            this.TextureSuffix = TextureSuffix;
        }

        internal void Enlarge(int NewSize)
        {
            if (Size < NewSize)
            {
                Size = NewSize;
            }
        }
    }
}