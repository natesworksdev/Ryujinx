using Silk.NET.OpenGL.Legacy;
using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;

namespace Ryujinx.Graphics.OpenGL
{
    static class EnumConversion
    {
        public static TextureWrapMode Convert(this AddressMode mode)
        {
            switch (mode)
            {
                case AddressMode.Clamp:
                    return TextureWrapMode.Clamp;
                case AddressMode.Repeat:
                    return TextureWrapMode.Repeat;
                case AddressMode.MirrorClamp:
                    return (TextureWrapMode)ExtTextureMirrorClamp.MirrorClampExt;
                case AddressMode.MirrorClampToEdge:
                    return (TextureWrapMode)ExtTextureMirrorClamp.MirrorClampToEdgeExt;
                case AddressMode.MirrorClampToBorder:
                    return (TextureWrapMode)ExtTextureMirrorClamp.MirrorClampToBorderExt;
                case AddressMode.ClampToBorder:
                    return TextureWrapMode.ClampToBorder;
                case AddressMode.MirroredRepeat:
                    return TextureWrapMode.MirroredRepeat;
                case AddressMode.ClampToEdge:
                    return TextureWrapMode.ClampToEdge;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(AddressMode)} enum value: {mode}.");

            return TextureWrapMode.Clamp;
        }

        public static NvBlendEquationAdvanced Convert(this AdvancedBlendOp op)
        {
            switch (op)
            {
                case AdvancedBlendOp.Zero:
                    return NvBlendEquationAdvanced.Zero;
                case AdvancedBlendOp.Src:
                    return NvBlendEquationAdvanced.SrcNv;
                case AdvancedBlendOp.Dst:
                    return NvBlendEquationAdvanced.DstNv;
                case AdvancedBlendOp.SrcOver:
                    return NvBlendEquationAdvanced.SrcOverNv;
                case AdvancedBlendOp.DstOver:
                    return NvBlendEquationAdvanced.DstOverNv;
                case AdvancedBlendOp.SrcIn:
                    return NvBlendEquationAdvanced.SrcInNv;
                case AdvancedBlendOp.DstIn:
                    return NvBlendEquationAdvanced.DstInNv;
                case AdvancedBlendOp.SrcOut:
                    return NvBlendEquationAdvanced.SrcOutNv;
                case AdvancedBlendOp.DstOut:
                    return NvBlendEquationAdvanced.DstOutNv;
                case AdvancedBlendOp.SrcAtop:
                    return NvBlendEquationAdvanced.SrcAtopNv;
                case AdvancedBlendOp.DstAtop:
                    return NvBlendEquationAdvanced.DstAtopNv;
                case AdvancedBlendOp.Xor:
                    return NvBlendEquationAdvanced.XorNv;
                case AdvancedBlendOp.Plus:
                    return NvBlendEquationAdvanced.PlusNv;
                case AdvancedBlendOp.PlusClamped:
                    return NvBlendEquationAdvanced.PlusClampedNv;
                case AdvancedBlendOp.PlusClampedAlpha:
                    return NvBlendEquationAdvanced.PlusClampedAlphaNv;
                case AdvancedBlendOp.PlusDarker:
                    return NvBlendEquationAdvanced.PlusDarkerNv;
                case AdvancedBlendOp.Multiply:
                    return NvBlendEquationAdvanced.MultiplyNv;
                case AdvancedBlendOp.Screen:
                    return NvBlendEquationAdvanced.ScreenNv;
                case AdvancedBlendOp.Overlay:
                    return NvBlendEquationAdvanced.OverlayNv;
                case AdvancedBlendOp.Darken:
                    return NvBlendEquationAdvanced.DarkenNv;
                case AdvancedBlendOp.Lighten:
                    return NvBlendEquationAdvanced.LightenNv;
                case AdvancedBlendOp.ColorDodge:
                    return NvBlendEquationAdvanced.ColordodgeNv;
                case AdvancedBlendOp.ColorBurn:
                    return NvBlendEquationAdvanced.ColorburnNv;
                case AdvancedBlendOp.HardLight:
                    return NvBlendEquationAdvanced.HardlightNv;
                case AdvancedBlendOp.SoftLight:
                    return NvBlendEquationAdvanced.SoftlightNv;
                case AdvancedBlendOp.Difference:
                    return NvBlendEquationAdvanced.DifferenceNv;
                case AdvancedBlendOp.Minus:
                    return NvBlendEquationAdvanced.MinusNv;
                case AdvancedBlendOp.MinusClamped:
                    return NvBlendEquationAdvanced.MinusClampedNv;
                case AdvancedBlendOp.Exclusion:
                    return NvBlendEquationAdvanced.ExclusionNv;
                case AdvancedBlendOp.Contrast:
                    return NvBlendEquationAdvanced.ContrastNv;
                case AdvancedBlendOp.Invert:
                    return NvBlendEquationAdvanced.Invert;
                case AdvancedBlendOp.InvertRGB:
                    return NvBlendEquationAdvanced.InvertRgbNv;
                case AdvancedBlendOp.InvertOvg:
                    return NvBlendEquationAdvanced.InvertOvgNv;
                case AdvancedBlendOp.LinearDodge:
                    return NvBlendEquationAdvanced.LineardodgeNv;
                case AdvancedBlendOp.LinearBurn:
                    return NvBlendEquationAdvanced.LinearburnNv;
                case AdvancedBlendOp.VividLight:
                    return NvBlendEquationAdvanced.VividlightNv;
                case AdvancedBlendOp.LinearLight:
                    return NvBlendEquationAdvanced.LinearlightNv;
                case AdvancedBlendOp.PinLight:
                    return NvBlendEquationAdvanced.PinlightNv;
                case AdvancedBlendOp.HardMix:
                    return NvBlendEquationAdvanced.HardmixNv;
                case AdvancedBlendOp.Red:
                    return NvBlendEquationAdvanced.RedNv;
                case AdvancedBlendOp.Green:
                    return NvBlendEquationAdvanced.GreenNv;
                case AdvancedBlendOp.Blue:
                    return NvBlendEquationAdvanced.BlueNv;
                case AdvancedBlendOp.HslHue:
                    return NvBlendEquationAdvanced.HslHueNv;
                case AdvancedBlendOp.HslSaturation:
                    return NvBlendEquationAdvanced.HslSaturationNv;
                case AdvancedBlendOp.HslColor:
                    return NvBlendEquationAdvanced.HslColorNv;
                case AdvancedBlendOp.HslLuminosity:
                    return NvBlendEquationAdvanced.HslLuminosityNv;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(AdvancedBlendOp)} enum value: {op}.");

            return NvBlendEquationAdvanced.Zero;
        }

        public static All Convert(this AdvancedBlendOverlap overlap)
        {
            switch (overlap)
            {
                case AdvancedBlendOverlap.Uncorrelated:
                    return All.UncorrelatedNv;
                case AdvancedBlendOverlap.Disjoint:
                    return All.DisjointNv;
                case AdvancedBlendOverlap.Conjoint:
                    return All.ConjointNv;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(AdvancedBlendOverlap)} enum value: {overlap}.");

            return All.UncorrelatedNv;
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
                    return PrimitiveType.QuadStrip;
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

        public static TransformFeedbackPrimitiveType ConvertToTfType(this PrimitiveTopology topology)
        {
            switch (topology)
            {
                case PrimitiveTopology.Points:
                    return TransformFeedbackPrimitiveType.Points;
                case PrimitiveTopology.Lines:
                case PrimitiveTopology.LineLoop:
                case PrimitiveTopology.LineStrip:
                case PrimitiveTopology.LinesAdjacency:
                case PrimitiveTopology.LineStripAdjacency:
                    return TransformFeedbackPrimitiveType.Lines;
                case PrimitiveTopology.Triangles:
                case PrimitiveTopology.TriangleStrip:
                case PrimitiveTopology.TriangleFan:
                case PrimitiveTopology.TrianglesAdjacency:
                case PrimitiveTopology.TriangleStripAdjacency:
                    return TransformFeedbackPrimitiveType.Triangles;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(PrimitiveTopology)} enum value: {topology}.");

            return TransformFeedbackPrimitiveType.Points;
        }

        public static OpenTK.Graphics.OpenGL.StencilOp Convert(this GAL.StencilOp op)
        {
            switch (op)
            {
                case GAL.StencilOp.Keep:
                case GAL.StencilOp.KeepGl:
                    return OpenTK.Graphics.OpenGL.StencilOp.Keep;
                case GAL.StencilOp.Zero:
                case GAL.StencilOp.ZeroGl:
                    return OpenTK.Graphics.OpenGL.StencilOp.Zero;
                case GAL.StencilOp.Replace:
                case GAL.StencilOp.ReplaceGl:
                    return OpenTK.Graphics.OpenGL.StencilOp.Replace;
                case GAL.StencilOp.IncrementAndClamp:
                case GAL.StencilOp.IncrementAndClampGl:
                    return OpenTK.Graphics.OpenGL.StencilOp.Incr;
                case GAL.StencilOp.DecrementAndClamp:
                case GAL.StencilOp.DecrementAndClampGl:
                    return OpenTK.Graphics.OpenGL.StencilOp.Decr;
                case GAL.StencilOp.Invert:
                case GAL.StencilOp.InvertGl:
                    return OpenTK.Graphics.OpenGL.StencilOp.Invert;
                case GAL.StencilOp.IncrementAndWrap:
                case GAL.StencilOp.IncrementAndWrapGl:
                    return OpenTK.Graphics.OpenGL.StencilOp.IncrWrap;
                case GAL.StencilOp.DecrementAndWrap:
                case GAL.StencilOp.DecrementAndWrapGl:
                    return OpenTK.Graphics.OpenGL.StencilOp.DecrWrap;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(GAL.StencilOp)} enum value: {op}.");

            return OpenTK.Graphics.OpenGL.StencilOp.Keep;
        }

        public static All Convert(this SwizzleComponent swizzleComponent)
        {
            switch (swizzleComponent)
            {
                case SwizzleComponent.Zero:
                    return All.Zero;
                case SwizzleComponent.One:
                    return All.One;
                case SwizzleComponent.Red:
                    return All.Red;
                case SwizzleComponent.Green:
                    return All.Green;
                case SwizzleComponent.Blue:
                    return All.Blue;
                case SwizzleComponent.Alpha:
                    return All.Alpha;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(SwizzleComponent)} enum value: {swizzleComponent}.");

            return All.Zero;
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

        public static NvViewportSwizzle Convert(this ViewportSwizzle swizzle)
        {
            switch (swizzle)
            {
                case ViewportSwizzle.PositiveX:
                    return NvViewportSwizzle.ViewportSwizzlePositiveXNv;
                case ViewportSwizzle.PositiveY:
                    return NvViewportSwizzle.ViewportSwizzlePositiveYNv;
                case ViewportSwizzle.PositiveZ:
                    return NvViewportSwizzle.ViewportSwizzlePositiveZNv;
                case ViewportSwizzle.PositiveW:
                    return NvViewportSwizzle.ViewportSwizzlePositiveWNv;
                case ViewportSwizzle.NegativeX:
                    return NvViewportSwizzle.ViewportSwizzleNegativeXNv;
                case ViewportSwizzle.NegativeY:
                    return NvViewportSwizzle.ViewportSwizzleNegativeYNv;
                case ViewportSwizzle.NegativeZ:
                    return NvViewportSwizzle.ViewportSwizzleNegativeZNv;
                case ViewportSwizzle.NegativeW:
                    return NvViewportSwizzle.ViewportSwizzleNegativeWNv;
            }

            Logger.Debug?.Print(LogClass.Gpu, $"Invalid {nameof(ViewportSwizzle)} enum value: {swizzle}.");

            return NvViewportSwizzle.ViewportSwizzlePositiveXNv;
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
