using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.Gpu.Image
{
    /// <summary>
    /// Texture validation result.
    /// </summary>
    enum TextureValidationResult
    {
        Valid,
        InvalidSize,
        InvalidTarget,
        InvalidFormat
    }

    /// <summary>
    /// Texture validation utilities.
    /// </summary>
    static class TextureValidation
    {
        /// <summary>
        /// Checks if the texture parameters are valid.
        /// </summary>
        /// <param name="info">Texture parameters</param>
        /// <returns>Validation result</returns>
        public static TextureValidationResult Validate(ref TextureInfo info)
        {
            bool validSize;

            switch (info.Target)
            {
                case Target.Texture1D:
                    validSize = (uint)info.Width <= Constants.MaxTextureSize;
                    break;
                case Target.Texture2D:
                    validSize = (uint)info.Width <= Constants.MaxTextureSize &&
                                (uint)info.Height <= Constants.MaxTextureSize;
                    break;
                case Target.Texture3D:
                    validSize = (uint)info.Width <= Constants.Max3DTextureSize &&
                                (uint)info.Height <= Constants.Max3DTextureSize &&
                                (uint)info.DepthOrLayers <= Constants.Max3DTextureSize;
                    break;
                case Target.Texture1DArray:
                    validSize = (uint)info.Width <= Constants.MaxTextureSize &&
                                (uint)info.DepthOrLayers <= Constants.MaxArrayTextureLayers;
                    break;
                case Target.Texture2DArray:
                    validSize = (uint)info.Width <= Constants.MaxTextureSize &&
                                (uint)info.Height <= Constants.MaxTextureSize &&
                                (uint)info.DepthOrLayers <= Constants.MaxArrayTextureLayers;
                    break;
                case Target.Texture2DMultisample:
                    validSize = (uint)info.Width <= Constants.MaxTextureSize &&
                                (uint)info.Height <= Constants.MaxTextureSize;
                    break;
                case Target.Texture2DMultisampleArray:
                    validSize = (uint)info.Width <= Constants.MaxTextureSize &&
                                (uint)info.Height <= Constants.MaxTextureSize &&
                                (uint)info.DepthOrLayers <= Constants.MaxArrayTextureLayers;
                    break;
                case Target.Cubemap:
                    validSize = (uint)info.Width <= Constants.MaxTextureSize &&
                                (uint)info.Height <= Constants.MaxTextureSize && info.Width == info.Height;
                    break;
                case Target.CubemapArray:
                    validSize = (uint)info.Width <= Constants.MaxTextureSize &&
                                (uint)info.Height <= Constants.MaxTextureSize &&
                                (uint)info.DepthOrLayers <= Constants.MaxArrayTextureLayers && info.Width == info.Height;
                    break;
                case Target.TextureBuffer:
                    validSize = (uint)info.Width <= Constants.MaxBufferTextureSize;
                    break;
                default:
                    return TextureValidationResult.InvalidTarget;
            }

            if (!validSize)
            {
                return TextureValidationResult.InvalidSize;
            }

            if (info.IsLinear && (uint)info.Width > (uint)info.Stride)
            {
                return TextureValidationResult.InvalidSize;
            }

            return TextureValidationResult.Valid;
        }

        /// <summary>
        /// Checks if a sampler can be used in combination with a given texture.
        /// </summary>
        /// <param name="info">Texture parameters</param>
        /// <param name="sampler">Sampler parameters</param>
        /// <returns>True if they can be used together, false otherwise</returns>
        public static bool IsSamplerCompatible(TextureInfo info, SamplerDescriptor sampler)
        {
            if (info.FormatInfo.Format.IsDepthOrStencil() != (sampler.UnpackCompareMode() == CompareMode.CompareRToTexture))
            {
                return false;
            }

            return true;
        }
    }
}