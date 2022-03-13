using Ryujinx.Graphics.Shader;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// Cached shader code for a single shader stage.
    /// </summary>
    class CachedShaderStage
    {
        /// <summary>
        /// Shader program information.
        /// </summary>
        public ShaderProgramInfo Info { get; }

        /// <summary>
        /// Maxwell binary shader code.
        /// </summary>
        public byte[] Code { get; }

        public byte[] Cb1Data { get; }

        /// <summary>
        /// Creates a new instace of the shader code holder.
        /// </summary>
        /// <param name="info">Shader program information</param>
        /// <param name="code">Maxwell binary shader code</param>
        public CachedShaderStage(ShaderProgramInfo info, byte[] code, byte[] cb1Data)
        {
            Info = info;
            Code = code;
            Cb1Data = cb1Data;
        }
    }
}