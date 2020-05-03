using Ryujinx.Graphics.GAL;
using System;

namespace Ryujinx.Graphics.Gpu.Shader
{
    class ShaderBundle : IDisposable
    {
        /// <summary>
        /// Host shader program object.
        /// </summary>
        public IProgram HostProgram { get; }

        /// <summary>
        /// Compiled shader for each shader stage.
        /// </summary>
        public ShaderCodeHolder[] Shaders { get; }

        /// <summary>
        /// Creates a new instance of the shader bundle.
        /// </summary>
        /// <param name="hostProgram">Host program with all the shader stages</param>
        /// <param name="shaders">Shaders</param>
        public ShaderBundle(IProgram hostProgram, params ShaderCodeHolder[] shaders)
        {
            HostProgram = hostProgram;
            Shaders = shaders;
        }

        /// <summary>
        /// Dispose of the host shader resources.
        /// </summary>
        public void Dispose()
        {
            HostProgram.Dispose();

            foreach (ShaderCodeHolder holder in Shaders)
            {
                holder?.HostShader.Dispose();
            }
        }
    }
}
