namespace Ryujinx.Common.Configuration.ConfigurationStateSection
{
    /// <summary>
    /// Graphics configuration section
    /// </summary>
    public class GraphicsSection
    {
        /// <summary>
        /// Max Anisotropy. Values range from 0 - 16. Set to -1 to let the game decide.
        /// </summary>
        public ReactiveObject<float> MaxAnisotropy { get; }

        /// <summary>
        /// Aspect Ratio applied to the renderer window.
        /// </summary>
        public ReactiveObject<AspectRatio> AspectRatio { get; }

        /// <summary>
        /// Resolution Scale. An integer scale applied to applicable render targets. Values 1-4, or -1 to use a custom floating point scale instead.
        /// </summary>
        public ReactiveObject<int> ResScale { get; }

        /// <summary>
        /// Custom Resolution Scale. A custom floating point scale applied to applicable render targets. Only active when Resolution Scale is -1.
        /// </summary>
        public ReactiveObject<float> ResScaleCustom { get; }

        /// <summary>
        /// Dumps shaders in this local directory
        /// </summary>
        public ReactiveObject<string> ShadersDumpPath { get; }

        /// <summary>
        /// Enables or disables Vertical Sync
        /// </summary>
        public ReactiveObject<bool> EnableVsync { get; }

        /// <summary>
        /// Enables or disables Shader cache
        /// </summary>
        public ReactiveObject<bool> EnableShaderCache { get; }

        public GraphicsSection()
        {
            ResScale = new ReactiveObject<int>();
            ResScaleCustom = new ReactiveObject<float>();
            MaxAnisotropy = new ReactiveObject<float>();
            AspectRatio = new ReactiveObject<AspectRatio>();
            ShadersDumpPath = new ReactiveObject<string>();
            EnableVsync = new ReactiveObject<bool>();
            EnableShaderCache = new ReactiveObject<bool>();
        }
    }
}
