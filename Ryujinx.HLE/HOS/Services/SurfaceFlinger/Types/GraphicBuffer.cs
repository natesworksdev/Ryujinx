using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvMap;
using System;

namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    struct GraphicBuffer : IFlattenable
    {
        public GraphicBufferHeader Header;
        public NvGraphicBuffer     Buffer;

        public int Width => Header.Width;
        public int Height => Header.Height;
        public PixelFormat Format => Header.Format;
        public int Usage => Header.Usage;

        public Rect ToRect()
        {
            return new Rect(Width, Height);
        }

        public void Flattern(Parcel parcel)
        {
            parcel.WriteUnmanagedType(ref Header);
            parcel.WriteUnmanagedType(ref Buffer);
        }

        public void Unflatten(Parcel parcel)
        {
            Header = parcel.ReadUnmanagedType<GraphicBufferHeader>();

            if (Header.IntsCount != 0x51)
            {
                throw new NotImplementedException($"Unexpected Graphic Buffer ints count (expected 0x51, found 0x{Header.IntsCount:x}");
            }

            Buffer = parcel.ReadUnmanagedType<NvGraphicBuffer>();
        }

        public void IncrementNvMapHandleRefCount(KProcess process)
        {
            NvMapDeviceFile.IncrementMapRefCount(process, Buffer.NvMapId);

            for (int i = 0; i < 3; i++)
            {
                NvMapDeviceFile.IncrementMapRefCount(process, Buffer.Surfaces[i].NvMapHandle);
            }
        }

        public void DecrementNvMapHandleRefCount(KProcess process)
        {
            NvMapDeviceFile.DecrementMapRefCount(process, Buffer.NvMapId);

            for (int i = 0; i < 3; i++)
            {
                NvMapDeviceFile.DecrementMapRefCount(process, Buffer.Surfaces[i].NvMapHandle);
            }
        }

        public uint GetFlattenedSize()
        {
            return 0x16C;
        }

        public uint GetFdCount()
        {
            return 0;
        }
    }
}