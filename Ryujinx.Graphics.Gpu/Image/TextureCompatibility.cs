using Ryujinx.Common;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Texture;
using System;

namespace Ryujinx.Graphics.Gpu.Image
{
    /// <summary>
    /// Texture format compatibility checks.
    /// </summary>
    static class TextureCompatibility
    {
        private enum FormatClass
        {
            Unclassified,
            BCn64,
            BCn128,
            Bc1Rgb,
            Bc1Rgba,
            Bc2,
            Bc3,
            Bc4,
            Bc5,
            Bc6,
            Bc7
        }

        /// <summary>
        /// Checks if two formats are compatible, according to the host API copy format compatibility rules.
        /// </summary>
        /// <param name="lhs">First comparand</param>
        /// <param name="rhs">Second comparand</param>
        /// <returns>True if the formats are compatible, false otherwise</returns>
        public static bool FormatCompatible(FormatInfo lhs, FormatInfo rhs)
        {
            if (IsDsFormat(lhs.Format) || IsDsFormat(rhs.Format))
            {
                return lhs.Format == rhs.Format;
            }

            if (lhs.Format.IsAstc() || rhs.Format.IsAstc())
            {
                return lhs.Format == rhs.Format;
            }

            if (lhs.IsCompressed && rhs.IsCompressed)
            {
                FormatClass lhsClass = GetFormatClass(lhs.Format);
                FormatClass rhsClass = GetFormatClass(rhs.Format);

                return lhsClass == rhsClass;
            }
            else
            {
                return lhs.BytesPerPixel == rhs.BytesPerPixel;
            }
        }

        /// <summary>
        /// Performs a comparison of two texture informations.
        /// This performs a strict comparison, used to check if two textures are equal.
        /// </summary>
        /// <param name="firstTextureInfo">Texture information to compare</param>
        /// <param name="secondTextureInfo">Texture information to compare with</param>
        /// <param name="flags">Comparison flags</param>
        /// <returns>True if the textures are strictly equal or similar, false otherwise</returns>
        public static bool IsPerfectMatch(TextureInfo firstTextureInfo, TextureInfo secondTextureInfo, TextureSearchFlags flags)
        {
            if (!FormatMatches(firstTextureInfo, secondTextureInfo, (flags & TextureSearchFlags.ForSampler) != 0, (flags & TextureSearchFlags.ForCopy) != 0))
            {
                return false;
            }

            if (!LayoutMatches(firstTextureInfo, secondTextureInfo))
            {
                return false;
            }

            if (!SizeMatches(firstTextureInfo, secondTextureInfo, (flags & TextureSearchFlags.Strict) == 0))
            {
                return false;
            }

            if ((flags & TextureSearchFlags.ForSampler) != 0 || (flags & TextureSearchFlags.Strict) != 0)
            {
                if (!SamplerParamsMatches(firstTextureInfo, secondTextureInfo))
                {
                    return false;
                }
            }

            if ((flags & TextureSearchFlags.ForCopy) != 0)
            {
                bool msTargetCompatible = firstTextureInfo.Target == Target.Texture2DMultisample && secondTextureInfo.Target == Target.Texture2D;

                if (!msTargetCompatible && !TargetAndSamplesCompatible(firstTextureInfo, secondTextureInfo))
                {
                    return false;
                }
            }
            else if (!TargetAndSamplesCompatible(firstTextureInfo, secondTextureInfo))
            {
                return false;
            }

            return firstTextureInfo.Address == secondTextureInfo.Address && firstTextureInfo.Levels == secondTextureInfo.Levels;
        }

        /// <summary>
        /// Checks if the texture format matches with the specified texture information.
        /// </summary>
        /// <param name="firstTextureInfo">Texture information to compare</param>
        /// <param name="secondTextureInfo">Texture information to compare with</param>
        /// <param name="forSampler">Indicates that the texture will be used for shader sampling</param>
        /// <param name="forCopy">Indicates that the texture will be used as copy source or target</param>
        /// <returns>True if the format matches, with the given comparison rules</returns>
        public static bool FormatMatches(TextureInfo firstTextureInfo, TextureInfo secondTextureInfo, bool forSampler, bool forCopy)
        {
            // D32F and R32F texture have the same representation internally,
            // however the R32F format is used to sample from depth textures.
            if (firstTextureInfo.FormatInfo.Format == Format.D32Float && secondTextureInfo.FormatInfo.Format == Format.R32Float && (forSampler || forCopy))
            {
                return true;
            }

            if (forCopy)
            {
                // The 2D engine does not support depth-stencil formats, so it will instead
                // use equivalent color formats. We must also consider them as compatible.
                if (firstTextureInfo.FormatInfo.Format == Format.S8Uint && secondTextureInfo.FormatInfo.Format == Format.R8Unorm)
                {
                    return true;
                }

                if (firstTextureInfo.FormatInfo.Format == Format.D16Unorm && secondTextureInfo.FormatInfo.Format == Format.R16Unorm)
                {
                    return true;
                }

                if ((firstTextureInfo.FormatInfo.Format == Format.D24UnormS8Uint ||
                     firstTextureInfo.FormatInfo.Format == Format.D24X8Unorm) && secondTextureInfo.FormatInfo.Format == Format.B8G8R8A8Unorm)
                {
                    return true;
                }
            }

            return firstTextureInfo.FormatInfo.Format == secondTextureInfo.FormatInfo.Format;
        }

        /// <summary>
        /// Checks if the texture layout specified matches with this texture layout.
        /// The layout information is composed of the Stride for linear textures, or GOB block size
        /// for block linear textures.
        /// </summary>
        /// <param name="firstTextureInfo">Texture information to compare</param>
        /// <param name="secondTextureInfo">Texture information to compare with</param>
        /// <returns>True if the layout matches, false otherwise</returns>
        public static bool LayoutMatches(TextureInfo firstTextureInfo, TextureInfo secondTextureInfo)
        {
            if (firstTextureInfo.IsLinear != secondTextureInfo.IsLinear)
            {
                return false;
            }

            // For linear textures, gob block sizes are ignored.
            // For block linear textures, the stride is ignored.
            if (secondTextureInfo.IsLinear)
            {
                return firstTextureInfo.Stride == secondTextureInfo.Stride;
            }
            else
            {
                return firstTextureInfo.GobBlocksInY == secondTextureInfo.GobBlocksInY &&
                       firstTextureInfo.GobBlocksInZ == secondTextureInfo.GobBlocksInZ;
            }
        }

        /// <summary>
        /// Checks if the view sizes of a two given texture informations match.
        /// </summary>
        /// <param name="firstTextureInfo">Texture information of the texture view</param>
        /// <param name="secondTextureInfo">Texture information of the texture view to match against</param>
        /// <param name="level">Mipmap level of the texture view in relation to this texture</param>
        /// <param name="isCopy">True to check for copy compatibility rather than view compatibility</param>
        /// <returns>True if the sizes are compatible, false otherwise</returns>
        public static bool ViewSizeMatches(TextureInfo firstTextureInfo, TextureInfo secondTextureInfo, int level, bool isCopy)
        {
            Size size = GetAlignedSize(firstTextureInfo, level);

            Size otherSize = GetAlignedSize(secondTextureInfo);

            // For copies, we can copy a subset of the 3D texture slices,
            // so the depth may be different in this case.
            if (!isCopy && secondTextureInfo.Target == Target.Texture3D && size.Depth != otherSize.Depth)
            {
                return false;
            }

            return size.Width  == otherSize.Width &&
                   size.Height == otherSize.Height;
        }

        /// <summary>
        /// Checks if the texture sizes of the supplied texture informations match.
        /// </summary>
        /// <param name="firstTextureInfo">Texture information to compare</param>
        /// <param name="secondTextureInfo">Texture information to compare with</param>
        /// <returns>True if the size matches, false otherwise</returns>
        public static bool SizeMatches(TextureInfo firstTextureInfo, TextureInfo secondTextureInfo)
        {
            return SizeMatches(firstTextureInfo, secondTextureInfo, alignSizes: false);
        }

        /// <summary>
        /// Checks if the texture sizes of the supplied texture informations match the given level
        /// </summary>
        /// <param name="firstTextureInfo">Texture information to compare</param>
        /// <param name="secondTextureInfo">Texture information to compare with</param>
        /// <param name="level">Mipmap level of this texture to compare with</param>
        /// <returns>True if the size matches with the level, false otherwise</returns>
        public static bool SizeMatches(TextureInfo firstTextureInfo, TextureInfo secondTextureInfo, int level)
        {
            return Math.Max(1, firstTextureInfo.Width >> level)      == secondTextureInfo.Width &&
                   Math.Max(1, firstTextureInfo.Height >> level)     == secondTextureInfo.Height &&
                   Math.Max(1, firstTextureInfo.GetDepth() >> level) == secondTextureInfo.GetDepth();
        }

        /// <summary>
        /// Checks if the texture sizes of the supplied texture informations match.
        /// </summary>
        /// <param name="firstTextureInfo">Texture information to compare</param>
        /// <param name="secondTextureInfo">Texture information to compare with</param>
        /// <param name="alignSizes">True to align the sizes according to the texture layout for comparison</param>
        /// <returns>True if the sizes matches, false otherwise</returns>
        private static bool SizeMatches(TextureInfo firstTextureInfo, TextureInfo secondTextureInfo, bool alignSizes)
        {
            if (firstTextureInfo.GetLayers() != secondTextureInfo.GetLayers())
            {
                return false;
            }

            if (alignSizes)
            {
                Size size0 = GetAlignedSize(firstTextureInfo);
                Size size1 = GetAlignedSize(secondTextureInfo);

                return size0.Width  == size1.Width &&
                       size0.Height == size1.Height &&
                       size0.Depth  == size1.Depth;
            }
            else
            {
                return firstTextureInfo.Width      == secondTextureInfo.Width &&
                       firstTextureInfo.Height     == secondTextureInfo.Height &&
                       firstTextureInfo.GetDepth() == secondTextureInfo.GetDepth();
            }
        }


        /// <summary>
        /// Gets the aligned sizes of the specified texture information.
        /// The alignment depends on the texture layout and format bytes per pixel.
        /// </summary>
        /// <param name="info">Texture information to calculate the aligned size from</param>
        /// <param name="level">Mipmap level for texture views</param>
        /// <returns>The aligned texture size</returns>
        public static Size GetAlignedSize(TextureInfo info, int level = 0)
        {
            int width = Math.Max(1, info.Width >> level);
            int height = Math.Max(1, info.Height >> level);

            if (info.IsLinear)
            {
                return SizeCalculator.GetLinearAlignedSize(
                    width,
                    height,
                    info.FormatInfo.BlockWidth,
                    info.FormatInfo.BlockHeight,
                    info.FormatInfo.BytesPerPixel);
            }
            else
            {
                int depth = Math.Max(1, info.GetDepth() >> level);

                return SizeCalculator.GetBlockLinearAlignedSize(
                    width,
                    height,
                    depth,
                    info.FormatInfo.BlockWidth,
                    info.FormatInfo.BlockHeight,
                    info.FormatInfo.BytesPerPixel,
                    info.GobBlocksInY,
                    info.GobBlocksInZ,
                    info.GobBlocksInTileX);
            }
        }

        /// <summary>
        /// Check if it's possible to create a view with the layout of the second texture information from the first.
        /// The layout information is composed of the Stride for linear textures, or GOB block size
        /// for block linear textures.
        /// </summary>
        /// <param name="firstTextureInfo">Texture information of the texture view</param>
        /// <param name="secondTextureInfo">Texture information of the texture view to compare against</param>
        /// <param name="level">Start level of the texture view, in relation with the first texture</param>
        /// <returns>True if the layout is compatible, false otherwise</returns>
        public static bool ViewLayoutCompatible(TextureInfo firstTextureInfo, TextureInfo secondTextureInfo, int level)
        {
            if (firstTextureInfo.IsLinear != secondTextureInfo.IsLinear)
            {
                return false;
            }

            // For linear textures, gob block sizes are ignored.
            // For block linear textures, the stride is ignored.
            if (secondTextureInfo.IsLinear)
            {
                int width = Math.Max(1, firstTextureInfo.Width >> level);

                int stride = width * firstTextureInfo.FormatInfo.BytesPerPixel;

                stride = BitUtils.AlignUp(stride, 32);

                return stride == secondTextureInfo.Stride;
            }
            else
            {
                int height = Math.Max(1, firstTextureInfo.Height >> level);
                int depth = Math.Max(1, firstTextureInfo.GetDepth() >> level);

                (int gobBlocksInY, int gobBlocksInZ) = SizeCalculator.GetMipGobBlockSizes(
                    height,
                    depth,
                    firstTextureInfo.FormatInfo.BlockHeight,
                    firstTextureInfo.GobBlocksInY,
                    firstTextureInfo.GobBlocksInZ);

                return gobBlocksInY == secondTextureInfo.GobBlocksInY &&
                       gobBlocksInZ == secondTextureInfo.GobBlocksInZ;
            }
        }



        /// <summary>
        /// Checks if the view format of the first texture format is compatible with the format of the second.
        /// In general, the formats are considered compatible if the bytes per pixel values are equal,
        /// but there are more complex rules for some formats, like compressed or depth-stencil formats.
        /// This follows the host API copy compatibility rules.
        /// </summary>
        /// <param name="firstTextureInfo">Texture information of the texture view</param>
        /// <param name="secondTextureInfo">Texture information of the texture view</param>
        /// <returns>True if the formats are compatible, false otherwise</returns>
        public static bool ViewFormatCompatible(TextureInfo firstTextureInfo, TextureInfo secondTextureInfo)
        {
            return FormatCompatible(firstTextureInfo.FormatInfo, secondTextureInfo.FormatInfo);
        }

        /// <summary>
        /// Check if the target of the first texture view information is compatible with the target of the second texture view information.
        /// This follows the host API target compatibility rules.
        /// </summary>
        /// <param name="firstTextureInfo">Texture information of the texture view</param
        /// <param name="secondTextureInfo">Texture information of the texture view</param>
        /// <param name="isCopy">True to check for copy rather than view compatibility</param>
        /// <returns>True if the targets are compatible, false otherwise</returns>
        public static bool ViewTargetCompatible(TextureInfo firstTextureInfo, TextureInfo secondTextureInfo, bool isCopy)
        {
            switch (firstTextureInfo.Target)
            {
                case Target.Texture1D:
                case Target.Texture1DArray:
                    return secondTextureInfo.Target == Target.Texture1D ||
                           secondTextureInfo.Target == Target.Texture1DArray;

                case Target.Texture2D:
                    return secondTextureInfo.Target == Target.Texture2D ||
                           secondTextureInfo.Target == Target.Texture2DArray;

                case Target.Texture2DArray:
                case Target.Cubemap:
                case Target.CubemapArray:
                    return secondTextureInfo.Target == Target.Texture2D ||
                           secondTextureInfo.Target == Target.Texture2DArray ||
                           secondTextureInfo.Target == Target.Cubemap ||
                           secondTextureInfo.Target == Target.CubemapArray;

                case Target.Texture2DMultisample:
                case Target.Texture2DMultisampleArray:
                    return secondTextureInfo.Target == Target.Texture2DMultisample ||
                           secondTextureInfo.Target == Target.Texture2DMultisampleArray;

                case Target.Texture3D:
                    return secondTextureInfo.Target == Target.Texture3D ||
                          (secondTextureInfo.Target == Target.Texture2D && isCopy);
            }

            return false;
        }

        /// <summary>
        /// Checks if the texture shader sampling parameters of two texture informations match.
        /// </summary>
        /// <param name="firstTextureInfo">Texture information to compare</param>
        /// <param name="secondTextureInfo">Texture information to compare with</param>
        /// <returns>True if the texture shader sampling parameters matches, false otherwise</returns>
        public static bool SamplerParamsMatches(TextureInfo firstTextureInfo, TextureInfo secondTextureInfo)
        {
            return firstTextureInfo.DepthStencilMode == secondTextureInfo.DepthStencilMode &&
                   firstTextureInfo.SwizzleR         == secondTextureInfo.SwizzleR &&
                   firstTextureInfo.SwizzleG         == secondTextureInfo.SwizzleG &&
                   firstTextureInfo.SwizzleB         == secondTextureInfo.SwizzleB &&
                   firstTextureInfo.SwizzleA         == secondTextureInfo.SwizzleA;
        }

        /// <summary>
        /// Check if the texture target and samples count (for multisampled textures) matches.
        /// </summary>
        /// <param name="first">Texture information to compare with</param>
        /// <param name="secondTextureInfo">Texture information to compare with</param>
        /// <returns>True if the texture target and samples count matches, false otherwise</returns>
        public static bool TargetAndSamplesCompatible(TextureInfo firstTextureInfo, TextureInfo secondTextureInfo)
        {
            return firstTextureInfo.Target     == secondTextureInfo.Target &&
                   firstTextureInfo.SamplesInX == secondTextureInfo.SamplesInX &&
                   firstTextureInfo.SamplesInY == secondTextureInfo.SamplesInY;
        }


        /// <summary>
        /// Gets the texture format class, for compressed textures, or Unclassified otherwise.
        /// </summary>
        /// <param name="format">The format</param>
        /// <returns>Format class</returns>
        private static FormatClass GetFormatClass(Format format)
        {
            switch (format)
            {
                case Format.Bc1RgbSrgb:
                case Format.Bc1RgbUnorm:
                    return FormatClass.Bc1Rgb;
                case Format.Bc1RgbaSrgb:
                case Format.Bc1RgbaUnorm:
                    return FormatClass.Bc1Rgba;
                case Format.Bc2Srgb:
                case Format.Bc2Unorm:
                    return FormatClass.Bc2;
                case Format.Bc3Srgb:
                case Format.Bc3Unorm:
                    return FormatClass.Bc3;
                case Format.Bc4Snorm:
                case Format.Bc4Unorm:
                    return FormatClass.Bc4;
                case Format.Bc5Snorm:
                case Format.Bc5Unorm:
                    return FormatClass.Bc5;
                case Format.Bc6HSfloat:
                case Format.Bc6HUfloat:
                    return FormatClass.Bc6;
                case Format.Bc7Srgb:
                case Format.Bc7Unorm:
                    return FormatClass.Bc7;
            }

            return FormatClass.Unclassified;
        }

        /// <summary>
        /// Checks if the format is a depth-stencil texture format.
        /// </summary>
        /// <param name="format">Format to check</param>
        /// <returns>True if the format is a depth-stencil format (including depth only), false otherwise</returns>
        private static bool IsDsFormat(Format format)
        {
            switch (format)
            {
                case Format.D16Unorm:
                case Format.D24X8Unorm:
                case Format.D24UnormS8Uint:
                case Format.D32Float:
                case Format.D32FloatS8Uint:
                case Format.S8Uint:
                    return true;
            }

            return false;
        }
    }
}