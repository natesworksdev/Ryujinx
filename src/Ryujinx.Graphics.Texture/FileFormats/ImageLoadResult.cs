namespace Ryujinx.Graphics.Texture.FileFormats
{
    public enum ImageLoadResult
    {
        Success,
        CorruptedHeader,
        CorruptedData,
        DataTooShort,
        OutputTooShort,
        UnsupportedFormat,
    }
}
