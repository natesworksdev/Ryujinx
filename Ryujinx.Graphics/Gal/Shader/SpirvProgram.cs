using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal.Shader
{
    public class SpirvProgram : ShaderProgram
    {
        public byte[] Bytecode { get; private set; }

        public IDictionary<string, int> Locations { get; private set; }

        public SpirvProgram(
            byte[]                      Bytecode,
            IDictionary<string, int>    Locations,
            IEnumerable<ShaderDeclInfo> Textures,
            IEnumerable<ShaderDeclInfo> Uniforms)
            : base(Textures, Uniforms)
        {
            this.Bytecode = Bytecode;
            this.Locations = Locations;
        }
    }
}