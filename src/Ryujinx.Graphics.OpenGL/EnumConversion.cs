using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.OpenGL.Legacy.Extensions.EXT;
using Silk.NET.OpenGL.Legacy.Extensions.NV;

namespace Ryujinx.Graphics.OpenGL
{
    static class EnumConversion
    {
        public static TextureWrapMode Convert(this AddressMode mode)
        {
            switch (mode)
            {
                case AddressMode.Clamp:
#pragma warning disable CS0618 // Type or member is obsolete
                    return TextureWrapMode.Clamp;
#pragma warning restore CS0618 // Type or member is obsolete
                case AddressMode.Repeat:
                    return TextureWrapMode.Repeat;
                case AddressMode.MirrorClamp:
                    return (TextureWrapMode)EXT.MirrorClampExt;
                case AddressMode.MirrorClampToEdge:
                    return (TextureWrapMode)EXT.MirrorClampToEdgeExt;
                case AddressMode.MirrorClampToBorder:
                    return (TextureWrapMode)EXT.MirrorClampToBorderExt;
                case AddressMode.ClampToBorder:
                    return TextureWrapMode.ClampToBorder;
                case AddressMode.MirroredRepeat:
                    return TextureWrapMode.MirroredRepeat;
                case AddressMode.ClampToEdge:
                    return TextureWrapMode.ClampToEdge;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(AddressMode)} enum value: {mode}.");

#pragma warning disable CS0618 // Type or member is obsolete
            return TextureWrapMode.Clamp;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public static NV Convert(this AdvancedBlendOp op)
        {
            switch (op)
            {
                case AdvancedBlendOp.Zero:
                    return NV.Zero;
                case AdvancedBlendOp.Src:
                    return NV.SrcNV;
                case AdvancedBlendOp.Dst:
                    return NV.DstNV;
                case AdvancedBlendOp.SrcOver:
                    return NV.SrcOverNV;
                case AdvancedBlendOp.DstOver:
                    return NV.DstOverNV;
                case AdvancedBlendOp.SrcIn:
                    return NV.SrcInNV;
                case AdvancedBlendOp.DstIn:
                    return NV.DstInNV;
                case AdvancedBlendOp.SrcOut:
                    return NV.SrcOutNV;
                case AdvancedBlendOp.DstOut:
                    return NV.DstOutNV;
                case AdvancedBlendOp.SrcAtop:
                    return NV.SrcAtopNV;
                case AdvancedBlendOp.DstAtop:
                    return NV.DstAtopNV;
                case AdvancedBlendOp.Xor:
                    return NV.XorNV;
                case AdvancedBlendOp.Plus:
                    return NV.PlusNV;
                case AdvancedBlendOp.PlusClamped:
                    return NV.PlusClampedNV;
                case AdvancedBlendOp.PlusClampedAlpha:
                    return NV.PlusClampedAlphaNV;
                case AdvancedBlendOp.PlusDarker:
                    return NV.PlusDarkerNV;
                case AdvancedBlendOp.Multiply:
                    return NV.MultiplyNV;
                case AdvancedBlendOp.Screen:
                    return NV.ScreenNV;
                case AdvancedBlendOp.Overlay:
                    return NV.OverlayNV;
                case AdvancedBlendOp.Darken:
                    return NV.DarkenNV;
                case AdvancedBlendOp.Lighten:
                    return NV.LightenNV;
                case AdvancedBlendOp.ColorDodge:
                    return NV.ColordodgeNV;
                case AdvancedBlendOp.ColorBurn:
                    return NV.ColorburnNV;
                case AdvancedBlendOp.HardLight:
                    return NV.HardlightNV;
                case AdvancedBlendOp.SoftLight:
                    return NV.SoftlightNV;
                case AdvancedBlendOp.Difference:
                    return NV.DifferenceNV;
                case AdvancedBlendOp.Minus:
                    return NV.MinusNV;
                case AdvancedBlendOp.MinusClamped:
                    return NV.MinusClampedNV;
                case AdvancedBlendOp.Exclusion:
                    return NV.ExclusionNV;
                case AdvancedBlendOp.Contrast:
                    return NV.ContrastNV;
                case AdvancedBlendOp.Invert:
                    return NV.Invert;
                case AdvancedBlendOp.InvertRGB:
                    return NV.InvertRgbNV;
                case AdvancedBlendOp.InvertOvg:
                    return NV.InvertOvgNV;
                case AdvancedBlendOp.LinearDodge:
                    return NV.LineardodgeNV;
                case AdvancedBlendOp.LinearBurn:
                    return NV.LinearburnNV;
                case AdvancedBlendOp.VividLight:
                    return NV.VividlightNV;
                case AdvancedBlendOp.LinearLight:
                    return NV.LinearlightNV;
                case AdvancedBlendOp.PinLight:
                    return NV.PinlightNV;
                case AdvancedBlendOp.HardMix:
                    return NV.HardmixNV;
                case AdvancedBlendOp.Red:
                    return NV.RedNV;
                case AdvancedBlendOp.Green:
                    return NV.GreenNV;
                case AdvancedBlendOp.Blue:
                    return NV.BlueNV;
                case AdvancedBlendOp.HslHue:
                    return NV.HslHueNV;
                case AdvancedBlendOp.HslSaturation:
                    return NV.HslSaturationNV;
                case AdvancedBlendOp.HslColor:
                    return NV.HslColorNV;
                case AdvancedBlendOp.HslLuminosity:
                    return NV.HslLuminosityNV;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(AdvancedBlendOp)} enum value: {op}.");

            return NV.Zero;
        }

        public static NV Convert(this AdvancedBlendOverlap overlap)
        {
            switch (overlap)
            {
                case AdvancedBlendOverlap.Uncorrelated:
                    return NV.UncorrelatedNV;
                case AdvancedBlendOverlap.Disjoint:
                    return NV.DisjointNV;
                case AdvancedBlendOverlap.Conjoint:
                    return NV.ConjointNV;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(AdvancedBlendOverlap)} enum value: {overlap}.");

            return NV.UncorrelatedNV;
        }

        public static GLEnum Convert(this BlendFactor factor)
        {
            switch (factor)
            {
                case BlendFactor.Zero:
                case BlendFactor.ZeroGl:
                    return GLEnum.Zero;
                case BlendFactor.One:
                case BlendFactor.OneGl:
                    return GLEnum.One;
                case BlendFactor.SrcColor:
                case BlendFactor.SrcColorGl:
                    return GLEnum.SrcColor;
                case BlendFactor.OneMinusSrcColor:
                case BlendFactor.OneMinusSrcColorGl:
                    return GLEnum.OneMinusSrcColor;
                case BlendFactor.SrcAlpha:
                case BlendFactor.SrcAlphaGl:
                    return GLEnum.SrcAlpha;
                case BlendFactor.OneMinusSrcAlpha:
                case BlendFactor.OneMinusSrcAlphaGl:
                    return GLEnum.OneMinusSrcAlpha;
                case BlendFactor.DstAlpha:
                case BlendFactor.DstAlphaGl:
                    return GLEnum.DstAlpha;
                case BlendFactor.OneMinusDstAlpha:
                case BlendFactor.OneMinusDstAlphaGl:
                    return GLEnum.OneMinusDstAlpha;
                case BlendFactor.DstColor:
                case BlendFactor.DstColorGl:
                    return GLEnum.DstColor;
                case BlendFactor.OneMinusDstColor:
                case BlendFactor.OneMinusDstColorGl:
                    return GLEnum.OneMinusDstColor;
                case BlendFactor.SrcAlphaSaturate:
                case BlendFactor.SrcAlphaSaturateGl:
                    return GLEnum.SrcAlphaSaturate;
                case BlendFactor.Src1Color:
                case BlendFactor.Src1ColorGl:
                    return GLEnum.Src1Color;
                case BlendFactor.OneMinusSrc1Color:
                case BlendFactor.OneMinusSrc1ColorGl:
                    return GLEnum.OneMinusSrc1Color;
                case BlendFactor.Src1Alpha:
                case BlendFactor.Src1AlphaGl:
                    return GLEnum.Src1Alpha;
                case BlendFactor.OneMinusSrc1Alpha:
                case BlendFactor.OneMinusSrc1AlphaGl:
                    return GLEnum.OneMinusSrc1Alpha;
                case BlendFactor.ConstantColor:
                    return GLEnum.ConstantColor;
                case BlendFactor.OneMinusConstantColor:
                    return GLEnum.OneMinusConstantColor;
                case BlendFactor.ConstantAlpha:
                    return GLEnum.ConstantAlpha;
                case BlendFactor.OneMinusConstantAlpha:
                    return GLEnum.OneMinusConstantAlpha;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(BlendFactor)} enum value: {factor}.");

            return GLEnum.Zero;
        }

        public static GLEnum Convert(this BlendOp op)
        {
            switch (op)
            {
                case BlendOp.Add:
                case BlendOp.AddGl:
                    return GLEnum.FuncAdd;
                case BlendOp.Minimum:
                case BlendOp.MinimumGl:
                    return GLEnum.Min;
                case BlendOp.Maximum:
                case BlendOp.MaximumGl:
                    return GLEnum.Max;
                case BlendOp.Subtract:
                case BlendOp.SubtractGl:
                    return GLEnum.FuncSubtract;
                case BlendOp.ReverseSubtract:
                case BlendOp.ReverseSubtractGl:
                    return GLEnum.FuncReverseSubtract;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(BlendOp)} enum value: {op}.");

            return GLEnum.FuncAdd;
        }

        public static TextureCompareMode Convert(this CompareMode mode)
        {
            switch (mode)
            {
                case CompareMode.None:
                    return TextureCompareMode.None;
                case CompareMode.CompareRToTexture:
                    return TextureCompareMode.CompareRefToTexture;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(CompareMode)} enum value: {mode}.");

            return TextureCompareMode.None;
        }

        public static GLEnum Convert(this CompareOp op)
        {
            switch (op)
            {
                case CompareOp.Never:
                case CompareOp.NeverGl:
                    return GLEnum.Never;
                case CompareOp.Less:
                case CompareOp.LessGl:
                    return GLEnum.Less;
                case CompareOp.Equal:
                case CompareOp.EqualGl:
                    return GLEnum.Equal;
                case CompareOp.LessOrEqual:
                case CompareOp.LessOrEqualGl:
                    return GLEnum.Lequal;
                case CompareOp.Greater:
                case CompareOp.GreaterGl:
                    return GLEnum.Greater;
                case CompareOp.NotEqual:
                case CompareOp.NotEqualGl:
                    return GLEnum.Notequal;
                case CompareOp.GreaterOrEqual:
                case CompareOp.GreaterOrEqualGl:
                    return GLEnum.Gequal;
                case CompareOp.Always:
                case CompareOp.AlwaysGl:
                    return GLEnum.Always;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(CompareOp)} enum value: {op}.");

            return GLEnum.Never;
        }

        public static GLEnum Convert(this DepthMode mode)
        {
            switch (mode)
            {
                case DepthMode.MinusOneToOne:
                    return GLEnum.NegativeOneToOne;
                case DepthMode.ZeroToOne:
                    return GLEnum.ZeroToOne;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(DepthMode)} enum value: {mode}.");

            return GLEnum.NegativeOneToOne;
        }

        public static GLEnum Convert(this DepthStencilMode mode)
        {
            switch (mode)
            {
                case DepthStencilMode.Depth:
                    return GLEnum.DepthComponent;
                case DepthStencilMode.Stencil:
                    return GLEnum.StencilIndex;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(DepthStencilMode)} enum value: {mode}.");

            return GLEnum.Depth;
        }

        public static GLEnum Convert(this Face face)
        {
            switch (face)
            {
                case Face.Back:
                    return GLEnum.Back;
                case Face.Front:
                    return GLEnum.Front;
                case Face.FrontAndBack:
                    return GLEnum.FrontAndBack;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(Face)} enum value: {face}.");

            return GLEnum.Back;
        }

        public static FrontFaceDirection Convert(this FrontFace frontFace)
        {
            switch (frontFace)
            {
                case FrontFace.Clockwise:
                    return FrontFaceDirection.CW;
                case FrontFace.CounterClockwise:
                    return FrontFaceDirection.Ccw;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(FrontFace)} enum value: {frontFace}.");

            return FrontFaceDirection.CW;
        }

        public static DrawElementsType Convert(this IndexType type)
        {
            switch (type)
            {
                case IndexType.UByte:
                    return DrawElementsType.UnsignedByte;
                case IndexType.UShort:
                    return DrawElementsType.UnsignedShort;
                case IndexType.UInt:
                    return DrawElementsType.UnsignedInt;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(IndexType)} enum value: {type}.");

            return DrawElementsType.UnsignedByte;
        }

        public static TextureMagFilter Convert(this MagFilter filter)
        {
            switch (filter)
            {
                case MagFilter.Nearest:
                    return TextureMagFilter.Nearest;
                case MagFilter.Linear:
                    return TextureMagFilter.Linear;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(MagFilter)} enum value: {filter}.");

            return TextureMagFilter.Nearest;
        }

        public static TextureMinFilter Convert(this MinFilter filter)
        {
            switch (filter)
            {
                case MinFilter.Nearest:
                    return TextureMinFilter.Nearest;
                case MinFilter.Linear:
                    return TextureMinFilter.Linear;
                case MinFilter.NearestMipmapNearest:
                    return TextureMinFilter.NearestMipmapNearest;
                case MinFilter.LinearMipmapNearest:
                    return TextureMinFilter.LinearMipmapNearest;
                case MinFilter.NearestMipmapLinear:
                    return TextureMinFilter.NearestMipmapLinear;
                case MinFilter.LinearMipmapLinear:
                    return TextureMinFilter.LinearMipmapLinear;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(MinFilter)} enum value: {filter}.");

            return TextureMinFilter.Nearest;
        }

        public static Silk.NET.OpenGL.Legacy.PolygonMode Convert(this GAL.PolygonMode mode)
        {
            switch (mode)
            {
                case GAL.PolygonMode.Point:
                    return Silk.NET.OpenGL.Legacy.PolygonMode.Point;
                case GAL.PolygonMode.Line:
                    return Silk.NET.OpenGL.Legacy.PolygonMode.Line;
                case GAL.PolygonMode.Fill:
                    return Silk.NET.OpenGL.Legacy.PolygonMode.Fill;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(GAL.PolygonMode)} enum value: {mode}.");

            return Silk.NET.OpenGL.Legacy.PolygonMode.Fill;
        }

        public static PrimitiveType Convert(this PrimitiveTopology topology)
        {
            switch (topology)
            {
                case PrimitiveTopology.Points:
                    return PrimitiveType.Points;
                case PrimitiveTopology.Lines:
                    return PrimitiveType.Lines;
                case PrimitiveTopology.LineLoop:
                    return PrimitiveType.LineLoop;
                case PrimitiveTopology.LineStrip:
                    return PrimitiveType.LineStrip;
                case PrimitiveTopology.Triangles:
                    return PrimitiveType.Triangles;
                case PrimitiveTopology.TriangleStrip:
                    return PrimitiveType.TriangleStrip;
                case PrimitiveTopology.TriangleFan:
                    return PrimitiveType.TriangleFan;
                case PrimitiveTopology.Quads:
                    return PrimitiveType.Quads;
                case PrimitiveTopology.QuadStrip:
#pragma warning disable CS0618 // Type or member is obsolete
                    return PrimitiveType.QuadStrip;
#pragma warning restore CS0618 // Type or member is obsolete
                case PrimitiveTopology.Polygon:
                    return PrimitiveType.TriangleFan;
                case PrimitiveTopology.LinesAdjacency:
                    return PrimitiveType.LinesAdjacency;
                case PrimitiveTopology.LineStripAdjacency:
                    return PrimitiveType.LineStripAdjacency;
                case PrimitiveTopology.TrianglesAdjacency:
                    return PrimitiveType.TrianglesAdjacency;
                case PrimitiveTopology.TriangleStripAdjacency:
                    return PrimitiveType.TriangleStripAdjacency;
                case PrimitiveTopology.Patches:
                    return PrimitiveType.Patches;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(PrimitiveTopology)} enum value: {topology}.");

            return PrimitiveType.Points;
        }

        public static PrimitiveType ConvertToTfType(this PrimitiveTopology topology)
        {
            switch (topology)
            {
                case PrimitiveTopology.Points:
                    return PrimitiveType.Points;
                case PrimitiveTopology.Lines:
                case PrimitiveTopology.LineLoop:
                case PrimitiveTopology.LineStrip:
                case PrimitiveTopology.LinesAdjacency:
                case PrimitiveTopology.LineStripAdjacency:
                    return PrimitiveType.Lines;
                case PrimitiveTopology.Triangles:
                case PrimitiveTopology.TriangleStrip:
                case PrimitiveTopology.TriangleFan:
                case PrimitiveTopology.TrianglesAdjacency:
                case PrimitiveTopology.TriangleStripAdjacency:
                    return PrimitiveType.Triangles;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(PrimitiveTopology)} enum value: {topology}.");

            return PrimitiveType.Points;
        }

        public static Silk.NET.OpenGL.Legacy.StencilOp Convert(this GAL.StencilOp op)
        {
            switch (op)
            {
                case GAL.StencilOp.Keep:
                case GAL.StencilOp.KeepGl:
                    return Silk.NET.OpenGL.Legacy.StencilOp.Keep;
                case GAL.StencilOp.Zero:
                case GAL.StencilOp.ZeroGl:
                    return Silk.NET.OpenGL.Legacy.StencilOp.Zero;
                case GAL.StencilOp.Replace:
                case GAL.StencilOp.ReplaceGl:
                    return Silk.NET.OpenGL.Legacy.StencilOp.Replace;
                case GAL.StencilOp.IncrementAndClamp:
                case GAL.StencilOp.IncrementAndClampGl:
                    return Silk.NET.OpenGL.Legacy.StencilOp.Incr;
                case GAL.StencilOp.DecrementAndClamp:
                case GAL.StencilOp.DecrementAndClampGl:
                    return Silk.NET.OpenGL.Legacy.StencilOp.Decr;
                case GAL.StencilOp.Invert:
                case GAL.StencilOp.InvertGl:
                    return Silk.NET.OpenGL.Legacy.StencilOp.Invert;
                case GAL.StencilOp.IncrementAndWrap:
                case GAL.StencilOp.IncrementAndWrapGl:
                    return Silk.NET.OpenGL.Legacy.StencilOp.IncrWrap;
                case GAL.StencilOp.DecrementAndWrap:
                case GAL.StencilOp.DecrementAndWrapGl:
                    return Silk.NET.OpenGL.Legacy.StencilOp.DecrWrap;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(GAL.StencilOp)} enum value: {op}.");

            return Silk.NET.OpenGL.Legacy.StencilOp.Keep;
        }

        public static GLEnum Convert(this SwizzleComponent swizzleComponent)
        {
            switch (swizzleComponent)
            {
                case SwizzleComponent.Zero:
                    return GLEnum.Zero;
                case SwizzleComponent.One:
                    return GLEnum.One;
                case SwizzleComponent.Red:
                    return GLEnum.Red;
                case SwizzleComponent.Green:
                    return GLEnum.Green;
                case SwizzleComponent.Blue:
                    return GLEnum.Blue;
                case SwizzleComponent.Alpha:
                    return GLEnum.Alpha;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(SwizzleComponent)} enum value: {swizzleComponent}.");

            return GLEnum.Zero;
        }

        public static CopyImageSubDataTarget ConvertToImageTarget(this Target target)
        {
            return (CopyImageSubDataTarget)target.Convert();
        }

        public static TextureTarget Convert(this Target target)
        {
            switch (target)
            {
                case Target.Texture1D:
                    return TextureTarget.Texture1D;
                case Target.Texture2D:
                    return TextureTarget.Texture2D;
                case Target.Texture3D:
                    return TextureTarget.Texture3D;
                case Target.Texture1DArray:
                    return TextureTarget.Texture1DArray;
                case Target.Texture2DArray:
                    return TextureTarget.Texture2DArray;
                case Target.Texture2DMultisample:
                    return TextureTarget.Texture2DMultisample;
                case Target.Texture2DMultisampleArray:
                    return TextureTarget.Texture2DMultisampleArray;
                case Target.Cubemap:
                    return TextureTarget.TextureCubeMap;
                case Target.CubemapArray:
                    return TextureTarget.TextureCubeMapArray;
                case Target.TextureBuffer:
                    return TextureTarget.TextureBuffer;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(Target)} enum value: {target}.");

            return TextureTarget.Texture2D;
        }

        public static NV Convert(this ViewportSwizzle swizzle)
        {
            switch (swizzle)
            {
                case ViewportSwizzle.PositiveX:
                    return NV.ViewportSwizzlePositiveXNV;
                case ViewportSwizzle.PositiveY:
                    return NV.ViewportSwizzlePositiveYNV;
                case ViewportSwizzle.PositiveZ:
                    return NV.ViewportSwizzlePositiveZNV;
                case ViewportSwizzle.PositiveW:
                    return NV.ViewportSwizzlePositiveWNV;
                case ViewportSwizzle.NegativeX:
                    return NV.ViewportSwizzleNegativeXNV;
                case ViewportSwizzle.NegativeY:
                    return NV.ViewportSwizzleNegativeYNV;
                case ViewportSwizzle.NegativeZ:
                    return NV.ViewportSwizzleNegativeZNV;
                case ViewportSwizzle.NegativeW:
                    return NV.ViewportSwizzleNegativeWNV;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(ViewportSwizzle)} enum value: {swizzle}.");

            return NV.ViewportSwizzlePositiveXNV;
        }

        public static GLEnum Convert(this LogicalOp op)
        {
            switch (op)
            {
                case LogicalOp.Clear:
                    return GLEnum.Clear;
                case LogicalOp.And:
                    return GLEnum.And;
                case LogicalOp.AndReverse:
                    return GLEnum.AndReverse;
                case LogicalOp.Copy:
                    return GLEnum.Copy;
                case LogicalOp.AndInverted:
                    return GLEnum.AndInverted;
                case LogicalOp.Noop:
                    return GLEnum.Noop;
                case LogicalOp.Xor:
                    return GLEnum.Xor;
                case LogicalOp.Or:
                    return GLEnum.Or;
                case LogicalOp.Nor:
                    return GLEnum.Nor;
                case LogicalOp.Equiv:
                    return GLEnum.Equiv;
                case LogicalOp.Invert:
                    return GLEnum.Invert;
                case LogicalOp.OrReverse:
                    return GLEnum.OrReverse;
                case LogicalOp.CopyInverted:
                    return GLEnum.CopyInverted;
                case LogicalOp.OrInverted:
                    return GLEnum.OrInverted;
                case LogicalOp.Nand:
                    return GLEnum.Nand;
                case LogicalOp.Set:
                    return GLEnum.Set;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(LogicalOp)} enum value: {op}.");

            return GLEnum.Never;
        }

        public static ShaderType Convert(this ShaderStage stage)
        {
            return stage switch
            {
                ShaderStage.Compute => ShaderType.ComputeShader,
                ShaderStage.Vertex => ShaderType.VertexShader,
                ShaderStage.TessellationControl => ShaderType.TessControlShader,
                ShaderStage.TessellationEvaluation => ShaderType.TessEvaluationShader,
                ShaderStage.Geometry => ShaderType.GeometryShader,
                ShaderStage.Fragment => ShaderType.FragmentShader,
                _ => ShaderType.VertexShader,
            };
        }
    }
}
