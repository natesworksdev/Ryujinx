using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Memory;
using Ryujinx.Graphics.Texture;
using System.Collections.Generic;

namespace Ryujinx.Graphics
{
    public class GpuResourceManager
    {
        private NvGpu Gpu;

        private ValueRangeSet<long> WritableResources;

        private HashSet<long>[] UploadedKeys;

        public GpuResourceManager(NvGpu Gpu)
        {
            this.Gpu = Gpu;

            WritableResources = new ValueRangeSet<long>();

            UploadedKeys = new HashSet<long>[(int)NvGpuBufferType.Count];

            for (int Index = 0; Index < UploadedKeys.Length; Index++)
            {
                UploadedKeys[Index] = new HashSet<long>();
            }
        }

        public void SendColorBuffer(NvGpuVmm Vmm, long Position, int Attachment, GalImage NewImage)
        {
            long Size = (uint)ImageUtils.GetSize(NewImage);

            MarkAsCached(Vmm, Position, Size, NvGpuBufferType.Texture);

            bool IsCached = Gpu.Renderer.Texture.TryGetImage(Position, out GalImage CachedImage);

            if (IsCached && CachedImage.SizeMatches(NewImage))
            {
                Gpu.Renderer.RenderTarget.Reinterpret(Position, NewImage);
                Gpu.Renderer.RenderTarget.BindColor(Position, Attachment, NewImage);

                return;
            }

            Gpu.Renderer.Texture.Create(Position, (int)Size, NewImage);

            Gpu.Renderer.RenderTarget.BindColor(Position, Attachment, NewImage);
        }

        public void SendZetaBuffer(NvGpuVmm Vmm, long Position, GalImage NewImage)
        {
            long Size = (uint)ImageUtils.GetSize(NewImage);

            MarkAsCached(Vmm, Position, Size, NvGpuBufferType.Texture);

            bool IsCached = Gpu.Renderer.Texture.TryGetImage(Position, out GalImage CachedImage);

            if (IsCached && CachedImage.SizeMatches(NewImage))
            {
                Gpu.Renderer.RenderTarget.Reinterpret(Position, NewImage);
                Gpu.Renderer.RenderTarget.BindZeta(Position, NewImage);

                return;
            }

            Gpu.Renderer.Texture.Create(Position, (int)Size, NewImage);

            Gpu.Renderer.RenderTarget.BindZeta(Position, NewImage);
        }

        public void SendTexture(NvGpuVmm Vmm, long Position, GalImage NewImage, int TexIndex = -1)
        {
            long Size = (uint)ImageUtils.GetSize(NewImage);

            if (!MemoryRegionModified(Vmm, Position, Size, NvGpuBufferType.Texture))
            {
                if (Gpu.Renderer.Texture.TryGetImage(Position, out GalImage CachedImage) && CachedImage.SizeMatches(NewImage))
                {
                    Gpu.Renderer.RenderTarget.Reinterpret(Position, NewImage);

                    if (TexIndex >= 0)
                    {
                        Gpu.Renderer.Texture.Bind(Position, TexIndex, NewImage);
                    }

                    return;
                }
            }

            byte[] Data = ImageUtils.ReadTexture(Vmm, NewImage, Position);

            Gpu.Renderer.Texture.Create(Position, Data, NewImage);

            if (TexIndex >= 0)
            {
                Gpu.Renderer.Texture.Bind(Position, TexIndex, NewImage);
            }
        }

        internal void AddWritableResource(long Position, long Size)
        {
            WritableResources.Add(new ValueRange<long>(Position, Position + Size, Position));
        }

        public void SynchronizeRange(NvGpuVmm Vmm, long Position, long Size)
        {
            ValueRange<long> Range = new ValueRange<long>(Position, Position + Size);

            ValueRange<long>[] Ranges = WritableResources.GetAllIntersections(Range);

            foreach (ValueRange<long> CachedRange in Ranges)
            {
                DownloadResourceToGuest(Vmm, CachedRange.Value);

                if (CachedRange.Value == Position)
                {
                    WritableResources.Remove(CachedRange);
                }
            }
        }

        private void DownloadResourceToGuest(NvGpuVmm Vmm, long Position)
        {
            if (!Gpu.Renderer.Texture.TryGetImage(Position, out GalImage Image))
            {
                return;
            }

            byte[] Data = Gpu.Renderer.RenderTarget.GetData(Position);

            ImageUtils.WriteTexture(Vmm, Image, Position, Data);
        }

        private void MarkAsCached(NvGpuVmm Vmm, long Position, long Size, NvGpuBufferType Type)
        {
            Vmm.IsRegionModified(Position, Size, Type);
        }

        private bool MemoryRegionModified(NvGpuVmm Vmm, long Position, long Size, NvGpuBufferType Type)
        {
            HashSet<long> Uploaded = UploadedKeys[(int)Type];

            if (!Uploaded.Add(Position))
            {
                return false;
            }

            return Vmm.IsRegionModified(Position, Size, Type);
        }

        public void ClearPbCache()
        {
            for (int Index = 0; Index < UploadedKeys.Length; Index++)
            {
                UploadedKeys[Index].Clear();
            }
        }
    }
}
