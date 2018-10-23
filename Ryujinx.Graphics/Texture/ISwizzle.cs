namespace Ryujinx.Graphics.Texture
{
    interface ISwizzle
    {
        int GetSwizzleOffset(int x, int y);
    }
}