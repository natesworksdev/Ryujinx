namespace Ryujinx.Graphics.Gal.Shader
{
    internal class ShaderIrCmnt : ShaderIrNode
    {
        public string Comment { get; private set; }

        public ShaderIrCmnt(string comment)
        {
            Comment = comment;
        }
    }
}