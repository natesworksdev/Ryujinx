using Silk.NET.OpenGL;
using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class Sampler : ISampler
    {
        public uint Handle { get; private set; }
        private GL _api;

        public Sampler(GL api, SamplerCreateInfo info)
        {
            _api = api;
            Handle = _api.GenSampler();

            _api.SamplerParameter(Handle, SamplerParameterI.MinFilter, (int)info.MinFilter.Convert());
            _api.SamplerParameter(Handle, SamplerParameterI.MagFilter, (int)info.MagFilter.Convert());

            if (HwCapabilities.SupportsSeamlessCubemapPerTexture)
            {
                _api.SamplerParameter(Handle, GLEnum.TextureCubeMapSeamless, info.SeamlessCubemap ? 1 : 0);
            }

            _api.SamplerParameter(Handle, SamplerParameterI.WrapS, (int)info.AddressU.Convert());
            _api.SamplerParameter(Handle, SamplerParameterI.WrapT, (int)info.AddressV.Convert());
            _api.SamplerParameter(Handle, SamplerParameterI.WrapR, (int)info.AddressP.Convert());

            _api.SamplerParameter(Handle, SamplerParameterI.CompareMode, (int)info.CompareMode.Convert());
            _api.SamplerParameter(Handle, SamplerParameterI.CompareFunc, (int)info.CompareOp.Convert());

            unsafe
            {
                float* borderColor = stackalloc float[4]
                {
                    info.BorderColor.Red,
                    info.BorderColor.Green,
                    info.BorderColor.Blue,
                    info.BorderColor.Alpha,
                };

                _api.SamplerParameter(Handle, SamplerParameterF.BorderColor, borderColor);
            }

            _api.SamplerParameter(Handle, SamplerParameterF.TextureMinLod, info.MinLod);
            _api.SamplerParameter(Handle, SamplerParameterF.TextureMaxLod, info.MaxLod);
            _api.SamplerParameter(Handle, SamplerParameterF.TextureLodBias, info.MipLodBias);

            _api.SamplerParameter(Handle, SamplerParameterF.MaxAnisotropy, info.MaxAnisotropy);
        }

        public void Bind(uint unit)
        {
            _api.BindSampler(unit, Handle);
        }

        public void Dispose()
        {
            if (Handle != 0)
            {
                _api.DeleteSampler(Handle);

                Handle = 0;
            }
        }
    }
}
