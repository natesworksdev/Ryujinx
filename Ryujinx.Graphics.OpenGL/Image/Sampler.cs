using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class Sampler : ISampler
    {
        private readonly SamplerCache _cache;
        private bool _disposed;
        private int _referenceCount;

        public SamplerCreateInfo Info { get; }

        public int Handle { get; private set; }

        public Sampler(SamplerCache cache, SamplerCreateInfo info)
        {
            _cache = cache;
            Info = info;
            _referenceCount = 1;

            Handle = GL.GenSampler();

            GL.SamplerParameter(Handle, SamplerParameterName.TextureMinFilter, (int)info.MinFilter.Convert());
            GL.SamplerParameter(Handle, SamplerParameterName.TextureMagFilter, (int)info.MagFilter.Convert());

            if (HwCapabilities.SupportsSeamlessCubemapPerTexture)
            {
                GL.SamplerParameter(Handle, (SamplerParameterName)ArbSeamlessCubemapPerTexture.TextureCubeMapSeamless, info.SeamlessCubemap ? 1 : 0);
            }

            GL.SamplerParameter(Handle, SamplerParameterName.TextureWrapS, (int)info.AddressU.Convert());
            GL.SamplerParameter(Handle, SamplerParameterName.TextureWrapT, (int)info.AddressV.Convert());
            GL.SamplerParameter(Handle, SamplerParameterName.TextureWrapR, (int)info.AddressP.Convert());

            GL.SamplerParameter(Handle, SamplerParameterName.TextureCompareMode, (int)info.CompareMode.Convert());
            GL.SamplerParameter(Handle, SamplerParameterName.TextureCompareFunc, (int)info.CompareOp.Convert());

            unsafe
            {
                float* borderColor = stackalloc float[4]
                {
                    info.BorderColor.Red,
                    info.BorderColor.Green,
                    info.BorderColor.Blue,
                    info.BorderColor.Alpha
                };

                GL.SamplerParameter(Handle, SamplerParameterName.TextureBorderColor, borderColor);
            }

            GL.SamplerParameter(Handle, SamplerParameterName.TextureMinLod,  info.MinLod);
            GL.SamplerParameter(Handle, SamplerParameterName.TextureMaxLod,  info.MaxLod);
            GL.SamplerParameter(Handle, SamplerParameterName.TextureLodBias, info.MipLodBias);

            GL.SamplerParameter(Handle, SamplerParameterName.TextureMaxAnisotropyExt, info.MaxAnisotropy);
        }

        public void Bind(int unit)
        {
            GL.BindSampler(unit, Handle);
        }

        public void IncrementReferenceCount()
        {
            _referenceCount++;
        }

        public void DecrementReferenceCount()
        {
            if (--_referenceCount == 0)
            {
                if (Handle != 0)
                {
                    GL.DeleteSampler(Handle);

                    Handle = 0;
                }

                _cache.Remove(this);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                DecrementReferenceCount();
                _disposed = true;
            }
        }
    }
}
