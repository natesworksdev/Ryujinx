namespace Ryujinx.Graphics.Gal.Shader
{
    struct GlslProgram
    {
        public string Code;

        public GlslDeclInfo[] ConstBuffers;
        public GlslDeclInfo[] Attributes;
    }
}