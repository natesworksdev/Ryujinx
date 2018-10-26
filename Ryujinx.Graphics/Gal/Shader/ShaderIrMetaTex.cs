namespace Ryujinx.Graphics.Gal.Shader
{
    internal class ShaderIrMetaTex : ShaderIrMeta
    {
        public int Elem { get; private set; }

        public ShaderIrMetaTex(int elem)
        {
            Elem = elem;
        }
    }
}