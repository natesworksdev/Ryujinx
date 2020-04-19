using Ryujinx.Graphics.Gpu;
using Ryujinx.HLE.HOS.Services.Nv.Types;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x24)]
    struct AndroidFence : IFlattenable
    {
        public int FenceCount;

        private byte _fenceStorageStart;

        private Span<byte> _storage => MemoryMarshal.CreateSpan(ref _fenceStorageStart, Unsafe.SizeOf<NvFence>() * 4);

        public Span<NvFence> NvFences => MemoryMarshal.Cast<byte, NvFence>(_storage);

        public static AndroidFence NoFence
        {
            get
            {
                AndroidFence fence = new AndroidFence
                {
                    FenceCount = 0
                };

                fence.NvFences[0].Id = NvFence.InvalidSyncPointId;

                return fence;
            }
        }

        public void AddFence(NvFence fence)
        {
            NvFences[FenceCount++] = fence;
        }

        public void WaitForever(GpuContext gpuContext)
        {
            Wait(gpuContext, Timeout.InfiniteTimeSpan);
        }

        public void Wait(GpuContext gpuContext, TimeSpan timeout)
        {
            for (int i = 0; i < FenceCount; i++)
            {
                NvFences[i].Wait(gpuContext, timeout);
            }
        }

        public uint GetFlattenedSize()
        {
            return (uint)Unsafe.SizeOf<AndroidFence>();
        }

        public uint GetFdCount()
        {
            return 0;
        }

        public void Flatten(Parcel parcel)
        {
            parcel.WriteUnmanagedType(ref this);
        }

        public void Unflatten(Parcel parcel)
        {
            this = parcel.ReadUnmanagedType<AndroidFence>();
        }
    }
}