namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// Persistent shader cache configuration manager.
    /// </summary>
    class ShaderCacheConfiguration
    {
        /// <summary>
        /// True when the persistent shader cache is enabled, false otherwise.
        /// </summary>
        internal bool Enabled => ShaderPath != null;

        /// <summary>
        /// Path where the shader cache files should be saved.
        /// When set to null, the persistent shader cache is disabled.
        /// </summary>
        public string ShaderPath { get; set; }
    }
}
