using Ryujinx.HLE.HOS.Services.SurfaceFlinger;
using System;

namespace Ryujinx.HLE.Ui
{
#pragma warning disable CS0659
    /// <summary>
    /// Information about the indirect layer that is being drawn to.
    /// </summary>
    class RenderingSurfaceInfo : IEquatable<RenderingSurfaceInfo>
    {
        public ColorFormat ColorFormat { get; }
        public uint Width { get; }
        public uint Height { get; }
        public uint Pitch { get; }
        public uint Size { get; }

        public RenderingSurfaceInfo(ColorFormat colorFormat, uint width, uint height, uint pitch, uint size)
        {
            ColorFormat = colorFormat;
            Width = width;
            Height = height;
            Pitch = pitch;
            Size = size;
        }

        public bool Equals(RenderingSurfaceInfo other)
        {
            return ColorFormat == other.ColorFormat &&
                   Width       == other.Width       &&
                   Height      == other.Height      &&
                   Pitch       == other.Pitch       &&
                   Size        == other.Size;
        }


        public override bool Equals(object obj)
        {
            return obj is RenderingSurfaceInfo info && Equals(info);
        }
    }
#pragma warning restore CS0659
}
