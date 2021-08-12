using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using Silk.NET.Vulkan;

namespace Ryujinx.Graphics.Vulkan
{
    static class EnumConversion
    {
        public static ShaderStageFlags Convert(this ShaderStage stage)
        {
            switch (stage)
            {
                case ShaderStage.Vertex:
                    return ShaderStageFlags.ShaderStageVertexBit;
                case ShaderStage.Geometry:
                    return ShaderStageFlags.ShaderStageGeometryBit;
                case ShaderStage.TessellationControl:
                    return ShaderStageFlags.ShaderStageTessellationControlBit;
                case ShaderStage.TessellationEvaluation:
                    return ShaderStageFlags.ShaderStageTessellationEvaluationBit;
                case ShaderStage.Fragment:
                    return ShaderStageFlags.ShaderStageFragmentBit;
                case ShaderStage.Compute:
                    return ShaderStageFlags.ShaderStageComputeBit;
            };

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(ShaderStage)} enum value: {stage}.");

            return 0;
        }

        public static SamplerAddressMode Convert(this AddressMode mode)
        {
            switch (mode)
            {
                case AddressMode.Clamp:
                    return SamplerAddressMode.ClampToBorder; // TODO: Should be clamp
                case AddressMode.Repeat:
                    return SamplerAddressMode.Repeat;
                case AddressMode.MirrorClamp:
                    return SamplerAddressMode.ClampToBorder; // TODO: Should be mirror clamp
                case AddressMode.MirrorClampToEdge:
                    return SamplerAddressMode.MirrorClampToEdgeKhr;
                case AddressMode.MirrorClampToBorder:
                    return SamplerAddressMode.ClampToBorder; // TODO: Should be mirror clamp to border
                case AddressMode.ClampToBorder:
                    return SamplerAddressMode.ClampToBorder;
                case AddressMode.MirroredRepeat:
                    return SamplerAddressMode.MirroredRepeat;
                case AddressMode.ClampToEdge:
                    return SamplerAddressMode.ClampToEdge;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(AddressMode)} enum value: {mode}.");

            return SamplerAddressMode.ClampToBorder; // TODO: Should be clamp
        }

        public static Silk.NET.Vulkan.BlendFactor Convert(this GAL.BlendFactor factor)
        {
            switch (factor)
            {
                case GAL.BlendFactor.Zero:
                case GAL.BlendFactor.ZeroGl:
                    return Silk.NET.Vulkan.BlendFactor.Zero;
                case GAL.BlendFactor.One:
                case GAL.BlendFactor.OneGl:
                    return Silk.NET.Vulkan.BlendFactor.One;
                case GAL.BlendFactor.SrcColor:
                case GAL.BlendFactor.SrcColorGl:
                    return Silk.NET.Vulkan.BlendFactor.SrcColor;
                case GAL.BlendFactor.OneMinusSrcColor:
                case GAL.BlendFactor.OneMinusSrcColorGl:
                    return Silk.NET.Vulkan.BlendFactor.OneMinusSrcColor;
                case GAL.BlendFactor.SrcAlpha:
                case GAL.BlendFactor.SrcAlphaGl:
                    return Silk.NET.Vulkan.BlendFactor.SrcAlpha;
                case GAL.BlendFactor.OneMinusSrcAlpha:
                case GAL.BlendFactor.OneMinusSrcAlphaGl:
                    return Silk.NET.Vulkan.BlendFactor.OneMinusSrcAlpha;
                case GAL.BlendFactor.DstAlpha:
                case GAL.BlendFactor.DstAlphaGl:
                    return Silk.NET.Vulkan.BlendFactor.DstAlpha;
                case GAL.BlendFactor.OneMinusDstAlpha:
                case GAL.BlendFactor.OneMinusDstAlphaGl:
                    return Silk.NET.Vulkan.BlendFactor.OneMinusDstAlpha;
                case GAL.BlendFactor.DstColor:
                case GAL.BlendFactor.DstColorGl:
                    return Silk.NET.Vulkan.BlendFactor.DstColor;
                case GAL.BlendFactor.OneMinusDstColor:
                case GAL.BlendFactor.OneMinusDstColorGl:
                    return Silk.NET.Vulkan.BlendFactor.OneMinusDstColor;
                case GAL.BlendFactor.SrcAlphaSaturate:
                case GAL.BlendFactor.SrcAlphaSaturateGl:
                    return Silk.NET.Vulkan.BlendFactor.SrcAlphaSaturate;
                case GAL.BlendFactor.Src1Color:
                case GAL.BlendFactor.Src1ColorGl:
                    return Silk.NET.Vulkan.BlendFactor.Src1Color;
                case GAL.BlendFactor.OneMinusSrc1Color:
                case GAL.BlendFactor.OneMinusSrc1ColorGl:
                    return Silk.NET.Vulkan.BlendFactor.OneMinusSrc1Color;
                case GAL.BlendFactor.Src1Alpha:
                case GAL.BlendFactor.Src1AlphaGl:
                    return Silk.NET.Vulkan.BlendFactor.Src1Alpha;
                case GAL.BlendFactor.OneMinusSrc1Alpha:
                case GAL.BlendFactor.OneMinusSrc1AlphaGl:
                    return Silk.NET.Vulkan.BlendFactor.OneMinusSrc1Alpha;
                case GAL.BlendFactor.ConstantColor:
                    return Silk.NET.Vulkan.BlendFactor.ConstantColor;
                case GAL.BlendFactor.OneMinusConstantColor:
                    return Silk.NET.Vulkan.BlendFactor.OneMinusConstantColor;
                case GAL.BlendFactor.ConstantAlpha:
                    return Silk.NET.Vulkan.BlendFactor.ConstantAlpha;
                case GAL.BlendFactor.OneMinusConstantAlpha:
                    return Silk.NET.Vulkan.BlendFactor.OneMinusConstantAlpha;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(GAL.BlendFactor)} enum value: {factor}.");

            return Silk.NET.Vulkan.BlendFactor.Zero;
        }

        public static Silk.NET.Vulkan.BlendOp Convert(this GAL.BlendOp op)
        {
            switch (op)
            {
                case GAL.BlendOp.Add:
                case GAL.BlendOp.AddGl:
                    return Silk.NET.Vulkan.BlendOp.Add;
                case GAL.BlendOp.Subtract:
                case GAL.BlendOp.SubtractGl:
                    return Silk.NET.Vulkan.BlendOp.Subtract;
                case GAL.BlendOp.ReverseSubtract:
                case GAL.BlendOp.ReverseSubtractGl:
                    return Silk.NET.Vulkan.BlendOp.ReverseSubtract;
                case GAL.BlendOp.Minimum:
                case GAL.BlendOp.MinimumGl:
                    return Silk.NET.Vulkan.BlendOp.Min;
                case GAL.BlendOp.Maximum:
                case GAL.BlendOp.MaximumGl:
                    return Silk.NET.Vulkan.BlendOp.Max;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(GAL.BlendOp)} enum value: {op}.");

            return Silk.NET.Vulkan.BlendOp.Add;
        }

        public static Silk.NET.Vulkan.CompareOp Convert(this GAL.CompareOp op)
        {
            switch (op)
            {
                case GAL.CompareOp.Never:
                case GAL.CompareOp.NeverGl:
                    return Silk.NET.Vulkan.CompareOp.Never;
                case GAL.CompareOp.Less:
                case GAL.CompareOp.LessGl:
                    return Silk.NET.Vulkan.CompareOp.Less;
                case GAL.CompareOp.Equal:
                case GAL.CompareOp.EqualGl:
                    return Silk.NET.Vulkan.CompareOp.Equal;
                case GAL.CompareOp.LessOrEqual:
                case GAL.CompareOp.LessOrEqualGl:
                    return Silk.NET.Vulkan.CompareOp.LessOrEqual;
                case GAL.CompareOp.Greater:
                case GAL.CompareOp.GreaterGl:
                    return Silk.NET.Vulkan.CompareOp.Greater;
                case GAL.CompareOp.NotEqual:
                case GAL.CompareOp.NotEqualGl:
                    return Silk.NET.Vulkan.CompareOp.NotEqual;
                case GAL.CompareOp.GreaterOrEqual:
                case GAL.CompareOp.GreaterOrEqualGl:
                    return Silk.NET.Vulkan.CompareOp.GreaterOrEqual;
                case GAL.CompareOp.Always:
                case GAL.CompareOp.AlwaysGl:
                    return Silk.NET.Vulkan.CompareOp.Always;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(GAL.CompareOp)} enum value: {op}.");

            return Silk.NET.Vulkan.CompareOp.Never;
        }

        public static CullModeFlags Convert(this Face face)
        {
            switch (face)
            {
                case Face.Back:
                    return CullModeFlags.CullModeBackBit;
                case Face.Front:
                    return CullModeFlags.CullModeFrontBit;
                case Face.FrontAndBack:
                    return CullModeFlags.CullModeFrontAndBack;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(Face)} enum value: {face}.");

            return CullModeFlags.CullModeBackBit;
        }

        public static Silk.NET.Vulkan.FrontFace Convert(this GAL.FrontFace frontFace)
        {
            // Flipped to account for origin differences.
            switch (frontFace)
            {
                case GAL.FrontFace.Clockwise:
                    return Silk.NET.Vulkan.FrontFace.CounterClockwise;
                case GAL.FrontFace.CounterClockwise:
                    return Silk.NET.Vulkan.FrontFace.Clockwise;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(GAL.FrontFace)} enum value: {frontFace}.");

            return Silk.NET.Vulkan.FrontFace.Clockwise;
        }

        public static Silk.NET.Vulkan.IndexType Convert(this GAL.IndexType type)
        {
            switch (type)
            {
                case GAL.IndexType.UByte:
                    return Silk.NET.Vulkan.IndexType.Uint8Ext;
                case GAL.IndexType.UShort:
                    return Silk.NET.Vulkan.IndexType.Uint16;
                case GAL.IndexType.UInt:
                    return Silk.NET.Vulkan.IndexType.Uint32;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(GAL.IndexType)} enum value: {type}.");

            return Silk.NET.Vulkan.IndexType.Uint16;
        }

        public static Filter Convert(this MagFilter filter)
        {
            switch (filter)
            {
                case MagFilter.Nearest:
                    return Filter.Nearest;
                case MagFilter.Linear:
                    return Filter.Linear;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(MagFilter)} enum value: {filter}.");

            return Filter.Nearest;
        }

        public static (Filter, SamplerMipmapMode) Convert(this MinFilter filter)
        {
            switch (filter)
            {
                case MinFilter.Nearest:
                    return (Filter.Nearest, SamplerMipmapMode.Nearest);
                case MinFilter.Linear:
                    return (Filter.Linear, SamplerMipmapMode.Nearest);
                case MinFilter.NearestMipmapNearest:
                    return (Filter.Nearest, SamplerMipmapMode.Nearest);
                case MinFilter.LinearMipmapNearest:
                    return (Filter.Linear, SamplerMipmapMode.Nearest);
                case MinFilter.NearestMipmapLinear:
                    return (Filter.Nearest, SamplerMipmapMode.Linear);
                case MinFilter.LinearMipmapLinear:
                    return (Filter.Linear, SamplerMipmapMode.Linear);
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(MinFilter)} enum value: {filter}.");

            return (Filter.Nearest, SamplerMipmapMode.Nearest);
        }

        public static Silk.NET.Vulkan.PrimitiveTopology Convert(this GAL.PrimitiveTopology topology)
        {
            switch (topology)
            {
                case GAL.PrimitiveTopology.Points:
                    return Silk.NET.Vulkan.PrimitiveTopology.PointList;
                case GAL.PrimitiveTopology.Lines:
                    return Silk.NET.Vulkan.PrimitiveTopology.LineList;
                case GAL.PrimitiveTopology.LineStrip:
                    return Silk.NET.Vulkan.PrimitiveTopology.LineStrip;
                case GAL.PrimitiveTopology.Triangles:
                    return Silk.NET.Vulkan.PrimitiveTopology.TriangleList;
                case GAL.PrimitiveTopology.TriangleStrip:
                    return Silk.NET.Vulkan.PrimitiveTopology.TriangleStrip;
                case GAL.PrimitiveTopology.TriangleFan:
                    return Silk.NET.Vulkan.PrimitiveTopology.TriangleFan;
                case GAL.PrimitiveTopology.LinesAdjacency:
                    return Silk.NET.Vulkan.PrimitiveTopology.LineListWithAdjacency;
                case GAL.PrimitiveTopology.LineStripAdjacency:
                    return Silk.NET.Vulkan.PrimitiveTopology.LineStripWithAdjacency;
                case GAL.PrimitiveTopology.TrianglesAdjacency:
                    return Silk.NET.Vulkan.PrimitiveTopology.TriangleListWithAdjacency;
                case GAL.PrimitiveTopology.TriangleStripAdjacency:
                    return Silk.NET.Vulkan.PrimitiveTopology.TriangleStripWithAdjacency;
                case GAL.PrimitiveTopology.Patches:
                    return Silk.NET.Vulkan.PrimitiveTopology.PatchList;
                case GAL.PrimitiveTopology.Quads: // Emulated with triangle fans.
                    return Silk.NET.Vulkan.PrimitiveTopology.TriangleFan;
                case GAL.PrimitiveTopology.QuadStrip: // Emulated with triangle strips.
                    return Silk.NET.Vulkan.PrimitiveTopology.TriangleStrip;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(GAL.PrimitiveTopology)} enum value: {topology}.");

            return Silk.NET.Vulkan.PrimitiveTopology.TriangleList;
        }

        public static Silk.NET.Vulkan.StencilOp Convert(this GAL.StencilOp op)
        {
            switch (op)
            {
                case GAL.StencilOp.Keep:
                    return Silk.NET.Vulkan.StencilOp.Keep;
                case GAL.StencilOp.Zero:
                    return Silk.NET.Vulkan.StencilOp.Zero;
                case GAL.StencilOp.Replace:
                    return Silk.NET.Vulkan.StencilOp.Replace;
                case GAL.StencilOp.IncrementAndClamp:
                    return Silk.NET.Vulkan.StencilOp.IncrementAndClamp;
                case GAL.StencilOp.DecrementAndClamp:
                    return Silk.NET.Vulkan.StencilOp.DecrementAndClamp;
                case GAL.StencilOp.Invert:
                    return Silk.NET.Vulkan.StencilOp.Invert;
                case GAL.StencilOp.IncrementAndWrap:
                    return Silk.NET.Vulkan.StencilOp.IncrementAndWrap;
                case GAL.StencilOp.DecrementAndWrap:
                    return Silk.NET.Vulkan.StencilOp.DecrementAndWrap;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(GAL.StencilOp)} enum value: {op}.");

            return Silk.NET.Vulkan.StencilOp.Keep;
        }

        public static ComponentSwizzle Convert(this SwizzleComponent swizzleComponent)
        {
            switch (swizzleComponent)
            {
                case SwizzleComponent.Zero:
                    return ComponentSwizzle.Zero;
                case SwizzleComponent.One:
                    return ComponentSwizzle.One;
                case SwizzleComponent.Red:
                    return ComponentSwizzle.R;
                case SwizzleComponent.Green:
                    return ComponentSwizzle.G;
                case SwizzleComponent.Blue:
                    return ComponentSwizzle.B;
                case SwizzleComponent.Alpha:
                    return ComponentSwizzle.A;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(SwizzleComponent)} enum value: {swizzleComponent}.");

            return ComponentSwizzle.Zero;
        }

        public static ImageType Convert(this Target target)
        {
            switch (target)
            {
                case Target.Texture1D:
                case Target.Texture1DArray:
                case Target.TextureBuffer:
                    return ImageType.ImageType1D;
                case Target.Texture2D:
                case Target.Texture2DArray:
                case Target.Texture2DMultisample:
                case Target.Rectangle:
                case Target.Cubemap:
                case Target.CubemapArray:
                    return ImageType.ImageType2D;
                case Target.Texture3D:
                    return ImageType.ImageType3D;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(Target)} enum value: {target}.");

            return ImageType.ImageType2D;
        }

        public static ImageViewType ConvertView(this Target target)
        {
            switch (target)
            {
                case Target.Texture1D:
                    return ImageViewType.ImageViewType1D;
                case Target.Texture2D:
                case Target.Texture2DMultisample:
                case Target.Rectangle:
                    return ImageViewType.ImageViewType2D;
                case Target.Texture3D:
                    return ImageViewType.ImageViewType3D;
                case Target.Texture1DArray:
                    return ImageViewType.ImageViewType1DArray;
                case Target.Texture2DArray:
                    return ImageViewType.ImageViewType2DArray;
                case Target.Cubemap:
                    return ImageViewType.Cube;
                case Target.CubemapArray:
                    return ImageViewType.CubeArray;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(Target)} enum value: {target}.");

            return ImageViewType.ImageViewType2D;
        }

        public static ImageAspectFlags ConvertAspectFlags(this GAL.Format format)
        {
            switch (format)
            {
                case GAL.Format.D16Unorm:
                case GAL.Format.D24X8Unorm:
                case GAL.Format.D32Float:
                    return ImageAspectFlags.ImageAspectDepthBit;
                case GAL.Format.S8Uint:
                    return ImageAspectFlags.ImageAspectStencilBit;
                case GAL.Format.D24UnormS8Uint:
                case GAL.Format.D32FloatS8Uint:
                    return ImageAspectFlags.ImageAspectDepthBit | ImageAspectFlags.ImageAspectStencilBit;
                default:
                    return ImageAspectFlags.ImageAspectColorBit;
            }
        }

        public static ImageAspectFlags ConvertAspectFlags(this GAL.Format format, DepthStencilMode depthStencilMode)
        {
            switch (format)
            {
                case GAL.Format.D16Unorm:
                case GAL.Format.D24X8Unorm:
                case GAL.Format.D32Float:
                    return ImageAspectFlags.ImageAspectDepthBit;
                case GAL.Format.S8Uint:
                    return ImageAspectFlags.ImageAspectStencilBit;
                case GAL.Format.D24UnormS8Uint:
                case GAL.Format.D32FloatS8Uint:
                    return depthStencilMode == DepthStencilMode.Stencil ? ImageAspectFlags.ImageAspectStencilBit : ImageAspectFlags.ImageAspectDepthBit;
                default:
                    return ImageAspectFlags.ImageAspectColorBit;
            }
        }

        public static LogicOp Convert(this LogicalOp op)
        {
            switch (op)
            {
                case LogicalOp.Clear:
                    return LogicOp.Clear;
                case LogicalOp.And:
                    return LogicOp.And;
                case LogicalOp.AndReverse:
                    return LogicOp.AndReverse;
                case LogicalOp.Copy:
                    return LogicOp.Copy;
                case LogicalOp.AndInverted:
                    return LogicOp.AndInverted;
                case LogicalOp.Noop:
                    return LogicOp.NoOp;
                case LogicalOp.Xor:
                    return LogicOp.Xor;
                case LogicalOp.Or:
                    return LogicOp.Or;
                case LogicalOp.Nor:
                    return LogicOp.Nor;
                case LogicalOp.Equiv:
                    return LogicOp.Equivalent;
                case LogicalOp.Invert:
                    return LogicOp.Invert;
                case LogicalOp.OrReverse:
                    return LogicOp.OrReverse;
                case LogicalOp.CopyInverted:
                    return LogicOp.CopyInverted;
                case LogicalOp.OrInverted:
                    return LogicOp.OrInverted;
                case LogicalOp.Nand:
                    return LogicOp.Nand;
                case LogicalOp.Set:
                    return LogicOp.Set;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(LogicalOp)} enum value: {op}.");

            return LogicOp.Copy;
        }
    }
}
