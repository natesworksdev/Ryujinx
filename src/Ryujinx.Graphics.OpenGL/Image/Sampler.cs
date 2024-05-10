using Silk.NET.OpenGL.Legacy;
using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class Sampler : ISampler
    {
        public uint Handle { get; private set; }
        private readonly OpenGLRenderer _gd;

        public Sampler(OpenGLRenderer gd, SamplerCreateInfo info)
        {
            _gd = gd;
            Handle = _gd.Api.GenSampler();

            _gd.Api.SamplerParameter(Handle, SamplerParameterI.MinFilter, (int)info.MinFilter.Convert());
            _gd.Api.SamplerParameter(Handle, SamplerParameterI.MagFilter, (int)info.MagFilter.Convert());

            if (_gd.Capabilities.SupportsSeamlessCubemapPerTexture)
            {
                _gd.Api.SamplerParameter(Handle, GLEnum.TextureCubeMapSeamless, info.SeamlessCubemap ? 1 : 0);
            }

            _gd.Api.SamplerParameter(Handle, SamplerParameterI.WrapS, (int)info.AddressU.Convert());
            _gd.Api.SamplerParameter(Handle, SamplerParameterI.WrapT, (int)info.AddressV.Convert());
            _gd.Api.SamplerParameter(Handle, SamplerParameterI.WrapR, (int)info.AddressP.Convert());

            _gd.Api.SamplerParameter(Handle, SamplerParameterI.CompareMode, (int)info.CompareMode.Convert());
            _gd.Api.SamplerParameter(Handle, SamplerParameterI.CompareFunc, (int)info.CompareOp.Convert());

            unsafe
            {
                float* borderColor = stackalloc float[4]
                {
                    info.BorderColor.Red,
                    info.BorderColor.Green,
                    info.BorderColor.Blue,
                    info.BorderColor.Alpha,
                };

                _gd.Api.SamplerParameter(Handle, SamplerParameterF.BorderColor, borderColor);
            }

            _gd.Api.SamplerParameter(Handle, SamplerParameterF.MinLod, info.MinLod);
            _gd.Api.SamplerParameter(Handle, SamplerParameterF.MaxLod, info.MaxLod);
            _gd.Api.SamplerParameter(Handle, SamplerParameterF.LodBias, info.MipLodBias);

            _gd.Api.SamplerParameter(Handle, SamplerParameterF.MaxAnisotropy, info.MaxAnisotropy);
        }

        public void Bind(uint unit)
        {
            _gd.Api.BindSampler(unit, Handle);
        }

        public void Dispose()
        {
            if (Handle != 0)
            {
                _gd.Api.DeleteSampler(Handle);

                Handle = 0;
            }
        }
    }
}
