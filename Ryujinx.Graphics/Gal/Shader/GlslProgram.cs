using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal.Shader
{
    struct GlslProgram
    {
        public string Code { get; private set; }

        public ICollection<ShaderDeclInfo> Textures { get; private set; }
        public ICollection<ShaderDeclInfo> Uniforms { get; private set; }

        public GlslProgram(
            string                      Code,
            ICollection<ShaderDeclInfo> Textures,
            ICollection<ShaderDeclInfo> Uniforms)
        {
            this.Code     = Code;
            this.Textures = Textures;
            this.Uniforms = Uniforms;
        }
    }
}