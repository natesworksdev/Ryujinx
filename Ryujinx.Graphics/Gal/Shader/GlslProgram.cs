using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal.Shader
{
    public class GlslProgram : ShaderProgram
    {
        public string Code { get; private set; }

        public GlslProgram(
            string                      Code,
            IEnumerable<ShaderDeclInfo> Textures,
            IEnumerable<ShaderDeclInfo> Uniforms)
            : base(Textures, Uniforms)
        {
            this.Code = Code;
        }
    }
}