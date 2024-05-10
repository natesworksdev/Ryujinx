using Silk.NET.OpenGL.Legacy;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.OpenGL
{
    public enum GpuVendor
    {
        Unknown,
        AmdWindows,
        AmdUnix,
        IntelWindows,
        IntelUnix,
        Nvidia,
    }

    readonly struct HardwareCapabilities
    {
        public readonly bool SupportsAlphaToCoverageDitherControl;
        public readonly bool SupportsAstcCompression;
        public readonly bool SupportsBlendEquationAdvanced;
        public readonly bool SupportsDrawTexture;
        public readonly bool SupportsFragmentShaderInterlock;
        public readonly bool SupportsFragmentShaderOrdering;
        public readonly bool SupportsGeometryShaderPassthrough;
        public readonly bool SupportsImageLoadFormatted;
        public readonly bool SupportsIndirectParameters;
        public readonly bool SupportsParallelShaderCompile;
        public readonly bool SupportsPolygonOffsetClamp;
        public readonly bool SupportsQuads;
        public readonly bool SupportsSeamlessCubemapPerTexture;
        public readonly bool SupportsShaderBallot;
        public readonly bool SupportsShaderViewportLayerArray;
        public readonly bool SupportsViewportArray2;
        public readonly bool SupportsTextureCompressionBptc;
        public readonly bool SupportsTextureCompressionRgtc;
        public readonly bool SupportsTextureCompressionS3tc;
        public readonly bool SupportsTextureShadowLod;
        public readonly bool SupportsViewportSwizzle;

        public bool SupportsMismatchingViewFormat => GpuVendor != GpuVendor.AmdWindows && GpuVendor != GpuVendor.IntelWindows;
        public bool SupportsNonConstantTextureOffset => GpuVendor == GpuVendor.Nvidia;
        public bool RequiresSyncFlush => GpuVendor == GpuVendor.AmdWindows || IsIntel;
        public bool UsePersistentBufferForFlush => GpuVendor == GpuVendor.AmdWindows || GpuVendor == GpuVendor.Nvidia;

        public readonly int MaximumComputeSharedMemorySize;
        public readonly int StorageBufferOffsetAlignment;
        public readonly int TextureBufferOffsetAlignment;

        public readonly float MaximumSupportedAnisotropy;

        public readonly GpuVendor GpuVendor;

        public HardwareCapabilities(
            bool supportsAlphaToCoverageDitherControl,
            bool supportsAstcCompression,
            bool supportsBlendEquationAdvanced,
            bool supportsDrawTexture,
            bool supportsFragmentShaderInterlock,
            bool supportsFragmentShaderOrdering,
            bool supportsGeometryShaderPassthrough,
            bool supportsImageLoadFormatted,
            bool supportsIndirectParameters,
            bool supportsParallelShaderCompile,
            bool supportsPolygonOffsetClamp,
            bool supportsQuads,
            bool supportsSeamlessCubemapPerTexture,
            bool supportsShaderBallot,
            bool supportsShaderViewportLayerArray,
            bool supportsViewportArray2,
            bool supportsTextureCompressionBptc,
            bool supportsTextureCompressionRgtc,
            bool supportsTextureCompressionS3Tc,
            bool supportsTextureShadowLod,
            bool supportsViewportSwizzle,
            int maximumComputeSharedMemorySize,
            int storageBufferOffsetAlignment,
            int textureBufferOffsetAlignment,
            float maximumSupportedAnisotropy,
            GpuVendor gpuVendor)
        {
            SupportsAlphaToCoverageDitherControl = supportsAlphaToCoverageDitherControl;
            SupportsAstcCompression = supportsAstcCompression;
            SupportsBlendEquationAdvanced = supportsBlendEquationAdvanced;
            SupportsDrawTexture = supportsDrawTexture;
            SupportsFragmentShaderInterlock = supportsFragmentShaderInterlock;
            SupportsFragmentShaderOrdering = supportsFragmentShaderOrdering;
            SupportsGeometryShaderPassthrough = supportsGeometryShaderPassthrough;
            SupportsImageLoadFormatted = supportsImageLoadFormatted;
            SupportsIndirectParameters = supportsIndirectParameters;
            SupportsParallelShaderCompile = supportsParallelShaderCompile;
            SupportsPolygonOffsetClamp = supportsPolygonOffsetClamp;
            SupportsQuads = supportsQuads;
            SupportsSeamlessCubemapPerTexture = supportsSeamlessCubemapPerTexture;
            SupportsShaderBallot = supportsShaderBallot;
            SupportsShaderViewportLayerArray = supportsShaderViewportLayerArray;
            SupportsViewportArray2 = supportsViewportArray2;
            SupportsTextureCompressionBptc = supportsTextureCompressionBptc;
            SupportsTextureCompressionRgtc = supportsTextureCompressionRgtc;
            SupportsTextureCompressionS3tc = supportsTextureCompressionS3Tc;
            SupportsTextureShadowLod = supportsTextureShadowLod;
            SupportsViewportSwizzle = supportsViewportSwizzle;
            MaximumComputeSharedMemorySize = maximumComputeSharedMemorySize;
            StorageBufferOffsetAlignment = storageBufferOffsetAlignment;
            TextureBufferOffsetAlignment = textureBufferOffsetAlignment;
            MaximumSupportedAnisotropy = maximumSupportedAnisotropy;
            GpuVendor = gpuVendor;
        }

        public bool IsIntel => GpuVendor == GpuVendor.IntelWindows || GpuVendor == GpuVendor.IntelUnix;

        public static unsafe bool HasExtension(GL api, string name)
        {
            int numExtensions = api.GetInteger(GetPName.NumExtensions);

            for (uint extension = 0; extension < numExtensions; extension++)
            {
                if (Marshal.PtrToStringAnsi((IntPtr)api.GetString(StringName.Extensions, extension)) == name)
                {
                    return true;
                }
            }

            return false;
        }

        public static unsafe GpuVendor GetGpuVendor(GL api)
        {
            string vendor = Marshal.PtrToStringAnsi((IntPtr)api.GetString(StringName.Vendor)).ToLowerInvariant();

            switch (vendor)
            {
                case "nvidia corporation":
                    return GpuVendor.Nvidia;
                case "intel":
                    {
                        string renderer = Marshal.PtrToStringAnsi((IntPtr)api.GetString(StringName.Renderer)).ToLowerInvariant();

                        return renderer.Contains("mesa") ? GpuVendor.IntelUnix : GpuVendor.IntelWindows;
                    }
                case "ati technologies inc.":
                case "advanced micro devices, inc.":
                    return GpuVendor.AmdWindows;
                case "amd":
                case "x.org":
                    return GpuVendor.AmdUnix;
                default:
                    return GpuVendor.Unknown;
            }
        }

        public static bool SupportsQuadsCheck(GL api)
        {
            api.GetError(); // Clear any existing error.
            api.Begin(PrimitiveType.Quads);
            api.End();

            return api.GetError() == GLEnum.NoError;
        }
    }
}
