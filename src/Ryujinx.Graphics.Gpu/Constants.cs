namespace Ryujinx.Graphics.Gpu
{
    /// <summary>
    /// Common Maxwell GPU constants.
    /// </summary>
    static class Constants
    {
        /// <summary>
        /// Maximum number of compute uniform buffers.
        /// </summary>
        /// <remarks>
        /// This does not reflect the hardware count, the API will emulate some constant buffers using
        /// global memory to make up for the low amount of compute constant buffers supported by hardware (only 8).
        /// </remarks>
        public const int TotalCpUniformBuffers = 17; // 8 hardware constant buffers + 9 emulated (14 available to the user).

        /// <summary>
        /// Maximum number of compute storage buffers.
        /// </summary>
        /// <remarks>
        /// The maximum number of storage buffers is API limited, the hardware supports an unlimited amount.
        /// </remarks>
        public const int TotalCpStorageBuffers = 16;

        /// <summary>
        /// Maximum number of graphics uniform buffers.
        /// </summary>
        public const int TotalGpUniformBuffers = 18;

        /// <summary>
        /// Maximum number of graphics storage buffers.
        /// </summary>
        /// <remarks>
        /// The maximum number of storage buffers is API limited, the hardware supports an unlimited amount.
        /// </remarks>
        public const int TotalGpStorageBuffers = 16;

        /// <summary>
        /// Maximum number of transform feedback buffers.
        /// </summary>
        public const int TotalTransformFeedbackBuffers = 4;

        /// <summary>
        /// Maximum number of render target color buffers.
        /// </summary>
        public const int TotalRenderTargets = 8;

        /// <summary>
        /// Number of shader stages.
        /// </summary>
        public const int ShaderStages = 5;

        /// <summary>
        /// Maximum number of vertex attributes.
        /// </summary>
        public const int TotalVertexAttribs = 16; // FIXME: Should be 32, but OpenGL only supports 16.

        /// <summary>
        /// Maximum number of vertex buffers.
        /// </summary>
        public const int TotalVertexBuffers = 16;

        /// <summary>
        /// Maximum number of viewports.
        /// </summary>
        public const int TotalViewports = 16;

        /// <summary>
        /// Maximum size of gl_ClipDistance array in shaders.
        /// </summary>
        public const int TotalClipDistances = 8;

        /// <summary>
        /// Byte alignment for texture stride.
        /// </summary>
        public const int StrideAlignment = 32;

        /// <summary>
        /// Byte alignment for block linear textures
        /// </summary>
        public const int GobAlignment = 64;

        /// <summary>
        /// Number of the uniform buffer reserved by the driver to store the storage buffer base addresses.
        /// </summary>
        public const int DriverReservedUniformBuffer = 0;

        /// <summary>
        /// Number of the uniform buffer reserved by the driver to store texture binding handles.
        /// </summary>
        public const int DriverReserveTextureBindingsBuffer = 2;

        /// <summary>
        /// Maximum size that an storage buffer is assumed to have when the correct size is unknown.
        /// </summary>
        public const ulong MaxUnknownStorageSize = 0x100000;

        /// <summary>
        /// Maximum width and height for 1D, 2D and cube textures, including array and multisample variants.
        /// </summary>
        public const int MaxTextureSize = 0x4000;

        /// <summary>
        /// Maximum width, height and depth for 3D textures.
        /// </summary>
        public const int Max3DTextureSize = 0x800;

        /// <summary>
        /// Maximum layers for array textures.
        /// </summary>
        public const int MaxArrayTextureLayers = 0x800;

        /// <summary>
        /// Maximum width (effectively the size in pixels) for buffer textures.
        /// </summary>
        public const int MaxBufferTextureSize = 0x8000000;

        /// <summary>
        /// Alignment in bytes for pitch linear textures.
        /// </summary>
        public const int LinearStrideAlignment = 0x20;
    }
}
