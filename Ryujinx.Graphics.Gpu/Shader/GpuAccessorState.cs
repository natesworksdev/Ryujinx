namespace Ryujinx.Graphics.Gpu.Shader
{
    struct GpuAccessorState
    {
        public ulong TexturePoolGpuVa { get; }
        public int TexturePoolMaximumId { get; }
        public int TextureBufferIndex { get; }
        public bool EarlyZForce { get; }

        public GpuAccessorState(ulong texturePoolGpuVa, int texturePoolMaximumId, int textureBufferIndex, bool earlyZForce)
        {
            TexturePoolGpuVa = texturePoolGpuVa;
            TexturePoolMaximumId = texturePoolMaximumId;
            TextureBufferIndex = textureBufferIndex;
            EarlyZForce = earlyZForce;
        }
    }
}