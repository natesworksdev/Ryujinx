namespace Ryujinx.HLE.HOS.Services.Vi
{
    enum SrcScalingMode
    {
        Freeze              = 0,
        ScaleToWindow       = 1,
        ScaleAndCrop        = 2,
        None                = 3,
        PreserveAspectRatio = 4
    }

    enum DstScalingMode
    {
        ScaleToWindow       = 0,
        ScaleAndCrop        = 1,
        None                = 2,
        Freeze              = 3,
        PreserveAspectRatio = 4
    }
}
