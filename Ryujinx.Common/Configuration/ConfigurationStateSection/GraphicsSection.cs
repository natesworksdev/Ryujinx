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
        public ReactiveObject<float> MaxAnisotropy { get; protected set; }

        /// <summary>
        /// Aspect Ratio applied to the renderer window.
        /// </summary>
        public ReactiveObject<AspectRatio> AspectRatio { get; protected set; }

        /// <summary>
        /// Resolution Scale. An integer scale applied to applicable render targets. Values 1-4, or -1 to use a custom floating point scale instead.
        /// </summary>
        public ReactiveObject<int> ResScale { get; protected set; }

        /// <summary>
        /// Custom Resolution Scale. A custom floating point scale applied to applicable render targets. Only active when Resolution Scale is -1.
        /// </summary>
        public ReactiveObject<float> ResScaleCustom { get; protected set; }

        /// <summary>
        /// Dumps shaders in this local directory
        /// </summary>
        public ReactiveObject<string> ShadersDumpPath { get; protected set; }

        /// <summary>
        /// Enables or disables Vertical Sync
        /// </summary>
        public ReactiveObject<bool> EnableVsync { get; protected set; }

        /// <summary>
        /// Enables or disables Shader cache
        /// </summary>
        public ReactiveObject<bool> EnableShaderCache { get; protected set; }

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
