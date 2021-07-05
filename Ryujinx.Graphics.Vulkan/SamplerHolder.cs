using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;

namespace Ryujinx.Graphics.Vulkan
{
    class SamplerHolder : ISampler
    {
        private readonly Auto<DisposableSampler> _sampler;

        public unsafe SamplerHolder(Vk api, Device device, GAL.SamplerCreateInfo info)
        {
            (Filter minFilter, SamplerMipmapMode mipFilter) = EnumConversion.Convert(info.MinFilter);

            var borderColor = GetConstrainedBorderColor(info.BorderColor);

            var samplerCreateInfo = new Silk.NET.Vulkan.SamplerCreateInfo()
            {
                SType = StructureType.SamplerCreateInfo,
                MagFilter = info.MagFilter.Convert(),
                MinFilter = minFilter,
                MipmapMode = mipFilter,
                AddressModeU = info.AddressU.Convert(),
                AddressModeV = info.AddressV.Convert(),
                AddressModeW = info.AddressP.Convert(),
                MipLodBias = info.MipLodBias,
                AnisotropyEnable = info.MaxAnisotropy != 1f,
                MaxAnisotropy = info.MaxAnisotropy,
                CompareEnable = info.CompareMode == CompareMode.CompareRToTexture,
                CompareOp = info.CompareOp.Convert(),
                MinLod = info.MinLod,
                MaxLod = info.MaxLod,
                BorderColor = borderColor,
                UnnormalizedCoordinates = false // TODO: Use unnormalized coordinates.
            };

            api.CreateSampler(device, samplerCreateInfo, null, out var sampler).ThrowOnError();

            _sampler = new Auto<DisposableSampler>(new DisposableSampler(api, device, sampler));
        }

        private static BorderColor GetConstrainedBorderColor(ColorF arbitraryBorderColor)
        {
            float r = arbitraryBorderColor.Red;
            float g = arbitraryBorderColor.Green;
            float b = arbitraryBorderColor.Blue;
            float a = arbitraryBorderColor.Alpha;

            if (r == 0f && g == 0f && b == 0f)
            {
                if (a == 1f)
                {
                    return BorderColor.FloatOpaqueBlack;
                }
                else if (a == 0f)
                {
                    return BorderColor.FloatTransparentBlack;
                }
            }
            else if (r == 1f && g == 1f && b == 1f && a == 1f)
            {
                return BorderColor.FloatOpaqueWhite;
            }

            // TODO: Find a way to pass the correct color...
            return BorderColor.FloatOpaqueBlack;
        }

        public Auto<DisposableSampler> GetSampler()
        {
            return _sampler;
        }

        public void Dispose()
        {
            _sampler.Dispose();
        }
    }
}
