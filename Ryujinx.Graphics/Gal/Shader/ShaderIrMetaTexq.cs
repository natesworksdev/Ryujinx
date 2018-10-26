namespace Ryujinx.Graphics.Gal.Shader
{
    internal class ShaderIrMetaTexq : ShaderIrMeta
    {
        public ShaderTexqInfo Info { get; private set; }

        public int Elem { get; private set; }

        public ShaderIrMetaTexq(ShaderTexqInfo info, int elem)
        {
            this.Info = info;
            this.Elem = elem;
        }
    }
}