using Silk.NET.OpenGL;
using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class Sampler : ISampler
    {
        public int Handle { get; private set; }

        public Sampler(SamplerCreateInfo info)
        {
            Handle = GL.GenSampler();

            GL.SamplerParameter(Handle, SamplerParameterI.MinFilter, (int)info.MinFilter.Convert());
            GL.SamplerParameter(Handle, SamplerParameterI.MagFilter, (int)info.MagFilter.Convert());

            if (HwCapabilities.SupportsSeamlessCubemapPerTexture)
            {
                GL.SamplerParameter(Handle, (SamplerParameterName)ArbSeamlessCubemapPerTexture.TextureCubeMapSeamless, info.SeamlessCubemap ? 1 : 0);
            }

            GL.SamplerParameter(Handle, SamplerParameterI.WrapS, (int)info.AddressU.Convert());
            GL.SamplerParameter(Handle, SamplerParameterI.WrapT, (int)info.AddressV.Convert());
            GL.SamplerParameter(Handle, SamplerParameterI.WrapR, (int)info.AddressP.Convert());

            GL.SamplerParameter(Handle, SamplerParameterI.CompareMode, (int)info.CompareMode.Convert());
            GL.SamplerParameter(Handle, SamplerParameterI.CompareFunc, (int)info.CompareOp.Convert());

            unsafe
            {
                float* borderColor = stackalloc float[4]
                {
                    info.BorderColor.Red,
                    info.BorderColor.Green,
                    info.BorderColor.Blue,
                    info.BorderColor.Alpha,
                };

                GL.SamplerParameter(Handle, SamplerParameterF.BorderColor, borderColor);
            }

            GL.SamplerParameter(Handle, SamplerParameterF.TextureMinLod, info.MinLod);
            GL.SamplerParameter(Handle, SamplerParameterF.TextureMaxLod, info.MaxLod);
            GL.SamplerParameter(Handle, SamplerParameterF.TextureLodBias, info.MipLodBias);

            GL.SamplerParameter(Handle, SamplerParameterF.MaxAnisotropy, info.MaxAnisotropy);
        }

        public void Bind(int unit)
        {
            GL.BindSampler(unit, Handle);
        }

        public void Dispose()
        {
            if (Handle != 0)
            {
                GL.DeleteSampler(Handle);

                Handle = 0;
            }
        }
    }
}
