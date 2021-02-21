namespace Ryujinx.Graphics.Gpu.Image
{
    /// <summary>
    /// One side of a two-way dependency between one texture view and another.
    /// Contains a reference to the handle owning the dependency, and the other dependency.
    /// </summary>
    class TextureDependency
    {
        public TextureGroupHandle Handle;
        public TextureDependency Other;
        public bool Dirty;

        public TextureDependency(TextureGroupHandle handle)
        {
            Handle = handle;
        }

        public void SignalModified()
        {
            Other.Dirty = true;
            Other.Handle.DeferCopy(Handle);
        }
    }
}
