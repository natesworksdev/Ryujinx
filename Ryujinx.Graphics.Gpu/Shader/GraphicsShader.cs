using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// Cached graphics shader code for all stages.
    /// </summary>
    class GraphicsShader
    {
        /// <summary>
        /// Host shader program object.
        /// </summary>
        public IProgram HostProgram { get; set; }

        /// <summary>
        /// Compiled shader for each shader stage.
        /// </summary>
        public ShaderProgram[] Shaders { get; }

        /// <summary>
        /// Creates a new instance of cached graphics shader.
        /// </summary>
        public GraphicsShader()
        {
            Shaders = new ShaderProgram[Constants.ShaderStages];
        }
    }
}