namespace Ryujinx.Graphics.Gal.Shader
{
    internal class ShaderIrOperAbuf : ShaderIrNode
    {
        public int Offs { get; private set; }

        public ShaderIrNode Vertex { get; private set; }

        public ShaderIrOperAbuf(int offs, ShaderIrNode vertex)
        {
            this.Offs   = offs;
            this.Vertex = vertex;
        }
    }
}