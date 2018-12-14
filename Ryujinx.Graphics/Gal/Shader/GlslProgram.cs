using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal.Shader
{
    public struct GlslProgram
    {
        public string Code { get; private set; }

        public ShaderDeclInfo GlobalMemory { get; private set; }

        public IEnumerable<ShaderDeclInfo> Textures { get; private set; }
        public IEnumerable<ShaderDeclInfo> Uniforms { get; private set; }

        public GlslProgram(
            string                      Code,
            ShaderDeclInfo              GlobalMemory,
            IEnumerable<ShaderDeclInfo> Textures,
            IEnumerable<ShaderDeclInfo> Uniforms)
        {
            this.Code         = Code;
            this.GlobalMemory = GlobalMemory;
            this.Textures     = Textures;
            this.Uniforms     = Uniforms;
        }
    }
}