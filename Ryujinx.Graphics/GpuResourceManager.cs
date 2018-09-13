using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Memory;
using Ryujinx.Graphics.Texture;
using System.Collections.Generic;

namespace Ryujinx.Graphics
{
    class GpuResourceManager
    {
        private NvGpu Gpu;

        private class GpuResource
        {
            public bool GpuWritable { get; set; }
        }

        private Dictionary<long, GpuResource> Resources;

        private HashSet<long>[] UploadedKeys;

        public GpuResourceManager(NvGpu Gpu)
        {
            this.Gpu = Gpu;

            Resources = new Dictionary<long, GpuResource>();

            UploadedKeys = new HashSet<long>[(int)NvGpuBufferType.Count];

            for (int Index = 0; Index < UploadedKeys.Length; Index++)
            {
                UploadedKeys[Index] = new HashSet<long>();
            }
        }

        public void SendColorBuffer(NvGpuVmm Vmm, long Key, int Attachment, GalImage NewImage)
        {
            if (Resources.TryGetValue(Key, out GpuResource Resource))
            {
                Resources.Remove(Key);
            }

            Resources.Add(Key, new GpuResource());

            long Size = (uint)ImageUtils.GetSize(NewImage);

            Gpu.Renderer.Texture.CreateFb(Key, Size, NewImage);
            Gpu.Renderer.RenderTarget.BindColor(Key, Attachment, NewImage);
        }

        public void SendTexture(NvGpuVmm Vmm, long Key, long TicPosition, int TexIndex)
        {
            if (Resources.TryGetValue(Key, out GpuResource Resource))
            {
                Resources.Remove(Key);
            }

            Resources.Add(Key, new GpuResource());

            GalImage NewImage = TextureFactory.MakeTexture(Vmm, TicPosition);

            long Size = (uint)ImageUtils.GetSize(NewImage);

            if (IsResourceCached(Vmm, Key, Size, NvGpuBufferType.Texture))
            {
                if (Gpu.Renderer.Texture.TryGetCachedTexture(Key, Size, out GalImage CachedImage))
                {
                    Gpu.Renderer.Texture.Bind(Key, TexIndex, NewImage);

                    return;
                }
            }

            byte[] Data = TextureFactory.GetTextureData(Vmm, TicPosition);

            Gpu.Renderer.Texture.Create(Key, Data, NewImage);
            Gpu.Renderer.Texture.Bind(Key, TexIndex, NewImage);
        }

        private bool IsResourceCached(NvGpuVmm Vmm, long Key, long Size, NvGpuBufferType Type)
        {
            HashSet<long> Uploaded = UploadedKeys[(int)Type];

            if (!Uploaded.Add(Key))
            {
                return true;
            }

            return !Vmm.IsRegionModified(Key, Size, Type);
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
