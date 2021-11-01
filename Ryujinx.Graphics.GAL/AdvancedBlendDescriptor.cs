namespace Ryujinx.Graphics.GAL
{
    public struct AdvancedBlendDescriptor
    {
        public AdvancedBlendMode Mode { get; }
        public AdvancedBlendOverlap Overlap { get; }
        public bool SrcPreMultiplied { get; }

        public AdvancedBlendDescriptor(AdvancedBlendMode mode, AdvancedBlendOverlap overlap, bool srcPreMultiplied)
        {
            Mode = mode;
            Overlap = overlap;
            SrcPreMultiplied = srcPreMultiplied;
        }
    }
}
