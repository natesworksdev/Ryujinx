using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// Cached compute shader code.
    /// </summary>
    class ComputeShader
    {
        /// <summary>
        /// Host shader program object.
        /// </summary>
        public IProgram HostProgram { get; }

        /// <summary>
        /// Cached shader.
        /// </summary>
        public ShaderProgram Shader { get; }

        /// <summary>
        /// Creates a new instance of the compute shader.
        /// </summary>
        /// <param name="hostProgram">Host shader program</param>
        /// <param name="shader">Cached shader</param>
        public ComputeShader(IProgram hostProgram, ShaderProgram shader)
        {
            HostProgram = hostProgram;
            Shader      = shader;
        }
    }
}