namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderIrMetaTex : ShaderIrMeta
    {
        public int Elem { get; private set; }

        public ShaderIrMetaTex(int elem)
        {
            this.Elem = elem;
        }
    }
}