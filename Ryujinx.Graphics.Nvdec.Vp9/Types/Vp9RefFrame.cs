namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal enum Vp9RefFrame
    {
        LastFlag = 1 << 0,
        GoldFlag = 1 << 1,
        AltFlag = 1 << 2
    }
}
