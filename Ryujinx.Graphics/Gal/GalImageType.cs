namespace Ryujinx.Graphics.Gal
{
    public enum GalImageTarget
    {
        _1d = 0,
        _2d = 1,
        _3d = 2,
        CubeMap = 3,
        _1dArray = 4,
        _2dArray = 5,
        _1dBuffer = 6,
        _2dNoMimap = 7, //GL_TEXTURE_RECTANGLE?
        CubeArray = 8,
    }
}