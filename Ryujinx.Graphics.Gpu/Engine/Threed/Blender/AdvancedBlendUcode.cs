using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.Gpu.Engine.Threed.Blender
{
    struct FixedFunctionAlpha
    {
        public static FixedFunctionAlpha Disabled => new FixedFunctionAlpha(BlendUcodeEnable.EnableRGBA, default, default, default);

        public BlendUcodeEnable Enable { get; }
        public BlendOp AlphaOp { get; }
        public BlendFactor AlphaSrcFactor { get; }
        public BlendFactor AlphaDstFactor { get; }

        private FixedFunctionAlpha(BlendUcodeEnable enable, BlendOp alphaOp, BlendFactor alphaSrc, BlendFactor alphaDst)
        {
            Enable = enable;
            AlphaOp = alphaOp;
            AlphaSrcFactor = alphaSrc;
            AlphaDstFactor = alphaDst;
        }

        public FixedFunctionAlpha(BlendOp alphaOp, BlendFactor alphaSrc, BlendFactor alphaDst) : this(BlendUcodeEnable.EnableRGB, alphaOp, alphaSrc, alphaDst)
        {
        }
    }

    delegate FixedFunctionAlpha GenUcodeFunc(ref UcodeAssembler asm);

    struct AdvancedBlendUcode
    {
        public AdvancedBlendMode Mode { get; }
        public AdvancedBlendOverlap Overlap { get; }
        public bool SrcPreMultiplied { get; }
        public FixedFunctionAlpha Alpha { get; }
        public uint[] Code { get; }
        public RgbFloat[] Constants { get; }

        public AdvancedBlendUcode(
            AdvancedBlendMode mode,
            AdvancedBlendOverlap overlap,
            bool srcPreMultiplied,
            GenUcodeFunc genFunc)
        {
            Mode = mode;
            Overlap = overlap;
            SrcPreMultiplied = srcPreMultiplied;

            UcodeAssembler asm = new UcodeAssembler();
            Alpha = genFunc(ref asm);
            Code = asm.GetCode();
            Constants = asm.GetConstants();
        }
    }
}