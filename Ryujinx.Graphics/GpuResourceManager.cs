using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Memory;
using Ryujinx.Graphics.Texture;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics
{
    public class GpuResourceManager
    {
        private enum ImageType
        {
            None,
            Texture,
            TextureMirror,
            ColorBuffer,
            ZetaBuffer
        }

        private NvGpu Gpu;

        private HashSet<long>[] UploadedKeys;

        private Dictionary<long, ImageType> ImageTypes;
        private Dictionary<long, int>      MirroredTextures;

        public GpuResourceManager(NvGpu Gpu)
        {
            this.Gpu = Gpu;

            UploadedKeys = new HashSet<long>[(int)NvGpuBufferType.Count];

            for (int Index = 0; Index < UploadedKeys.Length; Index++)
            {
                UploadedKeys[Index] = new HashSet<long>();
            }

            ImageTypes = new Dictionary<long, ImageType>();
            MirroredTextures = new Dictionary<long, int>();
        }

        public void SendColorBuffer(NvGpuVmm Vmm, long Position, int Attachment, GalImage NewImage)
        {
            long Size = (uint)ImageUtils.GetSize(NewImage);

            ImageTypes[Position] = ImageType.ColorBuffer;

            if (!TryReuse(Vmm, Position, NewImage))
            {
                Gpu.Renderer.Texture.Create(Position, (int)Size, NewImage);
            }

            Gpu.Renderer.RenderTarget.BindColor(Position, Attachment);
        }

        public void SendZetaBuffer(NvGpuVmm Vmm, long Position, GalImage NewImage)
        {
            long Size = (uint)ImageUtils.GetSize(NewImage);

            ImageTypes[Position] = ImageType.ZetaBuffer;

            if (!TryReuse(Vmm, Position, NewImage))
            {
                Gpu.Renderer.Texture.Create(Position, (int)Size, NewImage);
            }

            Gpu.Renderer.RenderTarget.BindZeta(Position);
        }

        public void SendTexture(NvGpuVmm Vmm, long Position, GalImage NewImage)
        {
            PrepareSendTexture(Vmm, Position, NewImage);

            ImageTypes[Position] = ImageType.Texture;
        }

        public bool TryGetTextureMirorLayer(long Position, out int Layer)
        {
            if (MirroredTextures.TryGetValue(Position, out Layer))
            {
                ImageType Type = ImageTypes[Position];

                if (Type != ImageType.Texture && Type != ImageType.TextureMirror)
                {
                    throw new InvalidOperationException();
                }

                return true;
            }

            Layer = -1;
            return false;
        }

        public void SetTextureMirror(long Position, int Layer)
        {
            ImageTypes[Position] = ImageType.TextureMirror;
            MirroredTextures[Position] = Layer;
        }

        private void PrepareSendTexture(NvGpuVmm Vmm, long Position, GalImage NewImage)
        {
            long Size = ImageUtils.GetSize(NewImage);

            bool SkipCheck = false;

            if (ImageTypes.TryGetValue(Position, out ImageType OldType))
            {
                if (OldType == ImageType.ColorBuffer || OldType == ImageType.ZetaBuffer)
                {
                    //Avoid data destruction
                    MemoryRegionModified(Vmm, Position, Size, NvGpuBufferType.Texture);

                    SkipCheck = true;
                }
            }

            if (SkipCheck || !MemoryRegionModified(Vmm, Position, Size, NvGpuBufferType.Texture))
            {
                if (TryReuse(Vmm, Position, NewImage))
                {
                    return;
                }
            }

            byte[] Data = ImageUtils.ReadTexture(Vmm, NewImage, Position);

            Gpu.Renderer.Texture.Create(Position, Data, NewImage);
        }

        private bool TryReuse(NvGpuVmm Vmm, long Position, GalImage NewImage)
        {
            if (Gpu.Renderer.Texture.TryGetImage(Position, out GalImage CachedImage) && CachedImage.SizeMatches(NewImage))
            {
                Gpu.Renderer.RenderTarget.Reinterpret(Position, NewImage);

                return true;
            }

            return false;
        }

        public bool MemoryRegionModified(NvGpuVmm Vmm, long Position, long Size, NvGpuBufferType Type)
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

        public void ClearPbCache(NvGpuBufferType Type)
        {
            UploadedKeys[(int)Type].Clear();
        }
    }
}
