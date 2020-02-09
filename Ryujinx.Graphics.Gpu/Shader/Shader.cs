using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// Cached compute shader code.
    /// </summary>
    class Shader
    {
        /// <summary>
        /// Host shader program object.
        /// </summary>
        public IProgram HostProgram { get; }

        /// <summary>
        /// Cached shader.
        /// </summary>
        public ShaderMeta Meta { get; }

        /// <summary>
        /// Creates a new instance of the compute shader.
        /// </summary>
        /// <param name="hostProgram">Host shader program</param>
        /// <param name="meta">Shader meta data</param>
        public Shader(IProgram hostProgram, ShaderMeta meta)
        {
            HostProgram = hostProgram;
            Meta        = meta;
        }
    }
}