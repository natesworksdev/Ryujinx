using Ryujinx.Graphics.Shader;
using System;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// Shader cache file structure.
    /// </summary>
    [Serializable]
    class ShaderCacheFileFormat
    {
        /// <summary>
        /// Hash of the guest shader code.
        /// </summary>
        public int Hash { get; }

        /// <summary>
        /// Guest shader code for all active stages.
        /// </summary>
        public byte[][] GuestCode { get; }

        /// <summary>
        /// Host binary shader code.
        /// </summary>
        public byte[] Code { get; }

        /// <summary>
        /// Shader program information.
        /// </summary>
        public ShaderProgramInfo[] Info { get; }

        /// <summary>
        /// Creates the shader cache file format structure.
        /// </summary>
        /// <param name="hash">Hash of the guest shader code</param>
        /// <param name="pack">Spans of guest shader code</param>
        /// <param name="code">Host binary shader code</param>
        /// <param name="info">Shader program information</param>
        public ShaderCacheFileFormat(int hash, ShaderPack pack, byte[] code, ShaderProgramInfo[] info)
        {
            Hash = hash;
            GuestCode = pack.ToArray();
            Code = code;
            Info = info;
        }
    }
}
