namespace Ryujinx.Graphics.Texture
{
    internal interface ISwizzle
    {
        int GetSwizzleOffset(int x, int y);
    }
}