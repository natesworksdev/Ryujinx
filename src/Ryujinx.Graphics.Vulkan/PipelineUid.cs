using Ryujinx.Common.Memory;
using Silk.NET.Vulkan;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Ryujinx.Graphics.Vulkan
{
    struct PipelineUid : IRefEquatable<PipelineUid>
    {
        public ulong Id0;
        public ulong Id1;
        public ulong Id2;
        public ulong Id3;

        public ulong Id4;
        public ulong Id5;
        public ulong Id6;
        public ulong Id7;

        public ulong Id8;
        public ulong Id9;

        private uint VertexAttributeDescriptionsCount => (byte)((Id6 >> 38) & 0xFF);
        private uint VertexBindingDescriptionsCount => (byte)((Id6 >> 46) & 0xFF);
        private uint ViewportsCount => (byte)((Id6 >> 54) & 0xFF);
        private uint ScissorsCount => (byte)((Id7 >> 0) & 0xFF);
        private uint ColorBlendAttachmentStateCount => (byte)((Id7 >> 8) & 0xFF);
        private bool HasDepthStencil => ((Id7 >> 63) & 0x1) != 0UL;

        public Array32<VertexInputAttributeDescription> VertexAttributeDescriptions;
        public Array33<VertexInputBindingDescription> VertexBindingDescriptions;
        public Array16<Viewport> Viewports;
        public Array16<Rect2D> Scissors;
        public Array8<PipelineColorBlendAttachmentState> ColorBlendAttachmentState;
        public Array9<Format> AttachmentFormats;

        public override bool Equals(object obj)
        {
            return obj is PipelineUid other && Equals(other);
        }

        public bool Equals(ref PipelineUid other)
        {
            if (!Unsafe.As<ulong, Vector256<byte>>(ref Id0).Equals(Unsafe.As<ulong, Vector256<byte>>(ref other.Id0)) ||
                !Unsafe.As<ulong, Vector256<byte>>(ref Id4).Equals(Unsafe.As<ulong, Vector256<byte>>(ref other.Id4)) ||
                !Unsafe.As<ulong, Vector128<byte>>(ref Id8).Equals(Unsafe.As<ulong, Vector128<byte>>(ref other.Id8)))
            {
                return false;
            }

            if (!SequenceEqual<VertexInputAttributeDescription>(VertexAttributeDescriptions.AsSpan(), other.VertexAttributeDescriptions.AsSpan(), VertexAttributeDescriptionsCount))
            {
                return false;
            }

            if (!SequenceEqual<VertexInputBindingDescription>(VertexBindingDescriptions.AsSpan(), other.VertexBindingDescriptions.AsSpan(), VertexBindingDescriptionsCount))
            {
                return false;
            }

            if (!SequenceEqual<PipelineColorBlendAttachmentState>(ColorBlendAttachmentState.AsSpan(), other.ColorBlendAttachmentState.AsSpan(), ColorBlendAttachmentStateCount))
            {
                return false;
            }

            if (!SequenceEqual<Format>(AttachmentFormats.AsSpan(), other.AttachmentFormats.AsSpan(), ColorBlendAttachmentStateCount + (HasDepthStencil ? 1u : 0u)))
            {
                return false;
            }

            return true;
        }

        private static bool SequenceEqual<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, uint count) where T : unmanaged
        {
            return MemoryMarshal.Cast<T, byte>(x.Slice(0, (int)count)).SequenceEqual(MemoryMarshal.Cast<T, byte>(y.Slice(0, (int)count)));
        }

        public override int GetHashCode()
        {
            ulong hash64 = Id0 * 23 ^
                           Id1 * 23 ^
                           Id2 * 23 ^
                           Id3 * 23 ^
                           Id4 * 23 ^
                           Id5 * 23 ^
                           Id6 * 23 ^
                           Id7 * 23 ^
                           Id8 * 23 ^
                           Id9 * 23;

            for (int i = 0; i < (int)VertexAttributeDescriptionsCount; i++)
            {
                hash64 ^= VertexAttributeDescriptions[i].Binding * 23;
                hash64 ^= (uint)VertexAttributeDescriptions[i].Format * 23;
                hash64 ^= VertexAttributeDescriptions[i].Location * 23;
                hash64 ^= VertexAttributeDescriptions[i].Offset * 23;
            }

            for (int i = 0; i < (int)VertexBindingDescriptionsCount; i++)
            {
                hash64 ^= VertexBindingDescriptions[i].Binding * 23;
                hash64 ^= (uint)VertexBindingDescriptions[i].InputRate * 23;
                hash64 ^= VertexBindingDescriptions[i].Stride * 23;
            }

            for (int i = 0; i < (int)ColorBlendAttachmentStateCount; i++)
            {
                hash64 ^= ColorBlendAttachmentState[i].BlendEnable * 23;
                hash64 ^= (uint)ColorBlendAttachmentState[i].SrcColorBlendFactor * 23;
                hash64 ^= (uint)ColorBlendAttachmentState[i].DstColorBlendFactor * 23;
                hash64 ^= (uint)ColorBlendAttachmentState[i].ColorBlendOp * 23;
                hash64 ^= (uint)ColorBlendAttachmentState[i].SrcAlphaBlendFactor * 23;
                hash64 ^= (uint)ColorBlendAttachmentState[i].DstAlphaBlendFactor * 23;
                hash64 ^= (uint)ColorBlendAttachmentState[i].AlphaBlendOp * 23;
                hash64 ^= (uint)ColorBlendAttachmentState[i].ColorWriteMask * 23;
            }

            for (int i = 0; i < (int)ColorBlendAttachmentStateCount; i++)
            {
                hash64 ^= (uint)AttachmentFormats[i] * 23;
            }

            return (int)hash64 ^ ((int)(hash64 >> 32) * 17);
        }

        public void TruncateVertexAttributeFormats()
        {
            // Vertex attributes exceeding the stride are invalid.
            // In metal, they cause glitches with the vertex shader fetching incorrect values.
            // To work around this, we reduce the format to something that doesn't exceed the stride if possible.
            // The assumption is that the exceeding components are not actually accessed on the shader.

            for (int index = 0; index < VertexAttributeDescriptionsCount; index++)
            {
                ref var attribute = ref VertexAttributeDescriptions[index];
                ref var vb = ref VertexBindingDescriptions[(int)attribute.Binding];

                Format format = attribute.Format;

                while (vb.Stride != 0 && attribute.Offset + GetAttributeFormatSize(format) > vb.Stride)
                {
                    Format newFormat = DropLastComponent(format);

                    if (newFormat == format)
                    {
                        // That case means we failed to find a format that fits within the stride,
                        // so just restore the original format and give up.
                        format = attribute.Format;
                        break;
                    }

                    format = newFormat;
                }

                attribute.Format = format;
            }
        }

        private static int GetAttributeFormatSize(Format format)
        {
            switch (format)
            {
                case Format.R8Unorm:
                case Format.R8SNorm:
                case Format.R8Uint:
                case Format.R8Sint:
                case Format.R8Uscaled:
                case Format.R8Sscaled:
                    return 1;

                case Format.R8G8Unorm:
                case Format.R8G8SNorm:
                case Format.R8G8Uint:
                case Format.R8G8Sint:
                case Format.R8G8Uscaled:
                case Format.R8G8Sscaled:
                case Format.R16Sfloat:
                case Format.R16Unorm:
                case Format.R16SNorm:
                case Format.R16Uint:
                case Format.R16Sint:
                case Format.R16Uscaled:
                case Format.R16Sscaled:
                    return 2;

                case Format.R8G8B8Unorm:
                case Format.R8G8B8SNorm:
                case Format.R8G8B8Uint:
                case Format.R8G8B8Sint:
                case Format.R8G8B8Uscaled:
                case Format.R8G8B8Sscaled:
                    return 3;

                case Format.R8G8B8A8Unorm:
                case Format.R8G8B8A8SNorm:
                case Format.R8G8B8A8Uint:
                case Format.R8G8B8A8Sint:
                case Format.R8G8B8A8Srgb:
                case Format.R8G8B8A8Uscaled:
                case Format.R8G8B8A8Sscaled:
                case Format.B8G8R8A8Unorm:
                case Format.B8G8R8A8Srgb:
                case Format.R16G16Sfloat:
                case Format.R16G16Unorm:
                case Format.R16G16SNorm:
                case Format.R16G16Uint:
                case Format.R16G16Sint:
                case Format.R16G16Uscaled:
                case Format.R16G16Sscaled:
                case Format.R32Sfloat:
                case Format.R32Uint:
                case Format.R32Sint:
                case Format.A2B10G10R10UnormPack32:
                case Format.A2B10G10R10UintPack32:
                case Format.B10G11R11UfloatPack32:
                case Format.E5B9G9R9UfloatPack32:
                case Format.A2B10G10R10SNormPack32:
                case Format.A2B10G10R10SintPack32:
                case Format.A2B10G10R10UscaledPack32:
                case Format.A2B10G10R10SscaledPack32:
                    return 4;

                case Format.R16G16B16Sfloat:
                case Format.R16G16B16Unorm:
                case Format.R16G16B16SNorm:
                case Format.R16G16B16Uint:
                case Format.R16G16B16Sint:
                case Format.R16G16B16Uscaled:
                case Format.R16G16B16Sscaled:
                    return 6;

                case Format.R16G16B16A16Sfloat:
                case Format.R16G16B16A16Unorm:
                case Format.R16G16B16A16SNorm:
                case Format.R16G16B16A16Uint:
                case Format.R16G16B16A16Sint:
                case Format.R16G16B16A16Uscaled:
                case Format.R16G16B16A16Sscaled:
                case Format.R32G32Sfloat:
                case Format.R32G32Uint:
                case Format.R32G32Sint:
                    return 8;

                case Format.R32G32B32Sfloat:
                case Format.R32G32B32Uint:
                case Format.R32G32B32Sint:
                    return 12;

                case Format.R32G32B32A32Sfloat:
                case Format.R32G32B32A32Uint:
                case Format.R32G32B32A32Sint:
                    return 16;
            }

            return 1;
        }

        private static Format DropLastComponent(Format format)
        {
            switch (format)
            {
                case Format.R8G8Unorm:
                    return Format.R8Unorm;
                case Format.R8G8SNorm:
                    return Format.R8SNorm;
                case Format.R8G8Uint:
                    return Format.R8Uint;
                case Format.R8G8Sint:
                    return Format.R8Sint;
                case Format.R8G8Uscaled:
                    return Format.R8Uscaled;
                case Format.R8G8Sscaled:
                    return Format.R8Sscaled;

                case Format.R8G8B8Unorm:
                    return Format.R8G8Unorm;
                case Format.R8G8B8SNorm:
                    return Format.R8G8SNorm;
                case Format.R8G8B8Uint:
                    return Format.R8G8Uint;
                case Format.R8G8B8Sint:
                    return Format.R8G8Sint;
                case Format.R8G8B8Uscaled:
                    return Format.R8G8Uscaled;
                case Format.R8G8B8Sscaled:
                    return Format.R8G8Sscaled;

                case Format.R8G8B8A8Unorm:
                    return Format.R8G8B8Unorm;
                case Format.R8G8B8A8SNorm:
                    return Format.R8G8B8SNorm;
                case Format.R8G8B8A8Uint:
                    return Format.R8G8B8Uint;
                case Format.R8G8B8A8Sint:
                    return Format.R8G8B8Sint;
                case Format.R8G8B8A8Srgb:
                    return Format.R8G8B8Srgb;
                case Format.R8G8B8A8Uscaled:
                    return Format.R8G8B8Uscaled;
                case Format.R8G8B8A8Sscaled:
                    return Format.R8G8B8Sscaled;
                case Format.B8G8R8A8Unorm:
                    return Format.R8G8B8Unorm;
                case Format.B8G8R8A8Srgb:
                    return Format.B8G8R8Srgb;

                case Format.R16G16Sfloat:
                    return Format.R16Sfloat;
                case Format.R16G16Unorm:
                    return Format.R16Unorm;
                case Format.R16G16SNorm:
                    return Format.R16SNorm;
                case Format.R16G16Uint:
                    return Format.R16Uint;
                case Format.R16G16Sint:
                    return Format.R16Sint;
                case Format.R16G16Uscaled:
                    return Format.R16Uscaled;
                case Format.R16G16Sscaled:
                    return Format.R16Sscaled;

                case Format.R16G16B16Sfloat:
                    return Format.R16G16Sfloat;
                case Format.R16G16B16Unorm:
                    return Format.R16G16Unorm;
                case Format.R16G16B16SNorm:
                    return Format.R16G16SNorm;
                case Format.R16G16B16Uint:
                    return Format.R16G16Uint;
                case Format.R16G16B16Sint:
                    return Format.R16G16Sint;
                case Format.R16G16B16Uscaled:
                    return Format.R16G16Uscaled;
                case Format.R16G16B16Sscaled:
                    return Format.R16G16Sscaled;

                case Format.R16G16B16A16Sfloat:
                    return Format.R16G16B16Sfloat;
                case Format.R16G16B16A16Unorm:
                    return Format.R16G16B16Unorm;
                case Format.R16G16B16A16SNorm:
                    return Format.R16G16B16SNorm;
                case Format.R16G16B16A16Uint:
                    return Format.R16G16B16Uint;
                case Format.R16G16B16A16Sint:
                    return Format.R16G16B16Sint;
                case Format.R16G16B16A16Uscaled:
                    return Format.R16G16B16Uscaled;
                case Format.R16G16B16A16Sscaled:
                    return Format.R16G16B16Sscaled;

                case Format.R32G32Sfloat:
                    return Format.R32Sfloat;
                case Format.R32G32Uint:
                    return Format.R32Uint;
                case Format.R32G32Sint:
                    return Format.R32Sint;

                case Format.R32G32B32Sfloat:
                    return Format.R32G32Sfloat;
                case Format.R32G32B32Uint:
                    return Format.R32G32Uint;
                case Format.R32G32B32Sint:
                    return Format.R32G32Sint;

                case Format.R32G32B32A32Sfloat:
                    return Format.R32G32B32Sfloat;
                case Format.R32G32B32A32Uint:
                    return Format.R32G32B32Uint;
                case Format.R32G32B32A32Sint:
                    return Format.R32G32B32Sint;
            }

            return format;
        }
    }
}
