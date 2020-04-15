namespace Ryujinx.Graphics.GAL
{
    public struct BlendDescriptor
    {
        public bool Enable { get; }

        public ColorF      ConstantBlend  { get; }
        public BlendOp     ColorOp        { get; }
        public BlendFactor ColorSrcFactor { get; }
        public BlendFactor ColorDstFactor { get; }
        public BlendOp     AlphaOp        { get; }
        public BlendFactor AlphaSrcFactor { get; }
        public BlendFactor AlphaDstFactor { get; }

        public BlendDescriptor(
            bool        enable,
            ColorF      constantBlend,
            BlendOp     colorOp,
            BlendFactor colorSrcFactor,
            BlendFactor colorDstFactor,
            BlendOp     alphaOp,
            BlendFactor alphaSrcFactor,
            BlendFactor alphaDstFactor)
        {
            Enable         = enable;
            ConstantBlend  = constantBlend;
            ColorOp        = colorOp;
            ColorSrcFactor = colorSrcFactor;
            ColorDstFactor = colorDstFactor;
            AlphaOp        = alphaOp;
            AlphaSrcFactor = alphaSrcFactor;
            AlphaDstFactor = alphaDstFactor;
        }
    }
}
