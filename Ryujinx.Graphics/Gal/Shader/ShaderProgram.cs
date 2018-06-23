using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal.Shader
{
    public class ShaderProgram
    {
        public IEnumerable<ShaderDeclInfo> Textures { get; private set; }
        public IEnumerable<ShaderDeclInfo> Uniforms { get; private set; }

        public ShaderProgram(
            IEnumerable<ShaderDeclInfo> Textures,
            IEnumerable<ShaderDeclInfo> Uniforms)
        {
            this.Textures = Textures;
            this.Uniforms = Uniforms;
        }
    }
}