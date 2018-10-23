using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Memory;
using Ryujinx.Graphics.Texture;
using System.Collections.Generic;

namespace Ryujinx.Graphics
{
    public class NvGpuEngine2D : INvGpuEngine
    {
        private enum CopyOperation
        {
            SrcCopyAnd,
            RopAnd,
            Blend,
            SrcCopy,
            Rop,
            SrcCopyPremult,
            BlendPremult
        }

        public int[] Registers { get; private set; }

        private NvGpu _gpu;

        private Dictionary<int, NvGpuMethod> _methods;

        public NvGpuEngine2D(NvGpu gpu)
        {
            this._gpu = gpu;

            Registers = new int[0xe00];

            _methods = new Dictionary<int, NvGpuMethod>();

            void AddMethod(int meth, int count, int stride, NvGpuMethod method)
            {
                while (count-- > 0)
                {
                    _methods.Add(meth, method);

                    meth += stride;
                }
            }

            AddMethod(0xb5, 1, 1, TextureCopy);
        }

        public void CallMethod(NvGpuVmm vmm, NvGpuPbEntry pbEntry)
        {
            if (_methods.TryGetValue(pbEntry.Method, out NvGpuMethod method))
            {
                method(vmm, pbEntry);
            }
            else
            {
                WriteRegister(pbEntry);
            }
        }

        private void TextureCopy(NvGpuVmm vmm, NvGpuPbEntry pbEntry)
        {
            CopyOperation operation = (CopyOperation)ReadRegister(NvGpuEngine2DReg.CopyOperation);

            int  srcFormat = ReadRegister(NvGpuEngine2DReg.SrcFormat);
            bool srcLinear = ReadRegister(NvGpuEngine2DReg.SrcLinear) != 0;
            int  srcWidth  = ReadRegister(NvGpuEngine2DReg.SrcWidth);
            int  srcHeight = ReadRegister(NvGpuEngine2DReg.SrcHeight);
            int  srcPitch  = ReadRegister(NvGpuEngine2DReg.SrcPitch);
            int  srcBlkDim = ReadRegister(NvGpuEngine2DReg.SrcBlockDimensions);

            int  dstFormat = ReadRegister(NvGpuEngine2DReg.DstFormat);
            bool dstLinear = ReadRegister(NvGpuEngine2DReg.DstLinear) != 0;
            int  dstWidth  = ReadRegister(NvGpuEngine2DReg.DstWidth);
            int  dstHeight = ReadRegister(NvGpuEngine2DReg.DstHeight);
            int  dstPitch  = ReadRegister(NvGpuEngine2DReg.DstPitch);
            int  dstBlkDim = ReadRegister(NvGpuEngine2DReg.DstBlockDimensions);

            GalImageFormat srcImgFormat = ImageUtils.ConvertSurface((GalSurfaceFormat)srcFormat);
            GalImageFormat dstImgFormat = ImageUtils.ConvertSurface((GalSurfaceFormat)dstFormat);

            GalMemoryLayout srcLayout = GetLayout(srcLinear);
            GalMemoryLayout dstLayout = GetLayout(dstLinear);

            int srcBlockHeight = 1 << ((srcBlkDim >> 4) & 0xf);
            int dstBlockHeight = 1 << ((dstBlkDim >> 4) & 0xf);

            long srcAddress = MakeInt64From2XInt32(NvGpuEngine2DReg.SrcAddress);
            long dstAddress = MakeInt64From2XInt32(NvGpuEngine2DReg.DstAddress);

            long srcKey = vmm.GetPhysicalAddress(srcAddress);
            long dstKey = vmm.GetPhysicalAddress(dstAddress);

            GalImage srcTexture = new GalImage(
                srcWidth,
                srcHeight, 1,
                srcBlockHeight,
                srcLayout,
                srcImgFormat);

            GalImage dstTexture = new GalImage(
                dstWidth,
                dstHeight, 1,
                dstBlockHeight,
                dstLayout,
                dstImgFormat);

            _gpu.ResourceManager.SendTexture(vmm, srcKey, srcTexture);
            _gpu.ResourceManager.SendTexture(vmm, dstKey, dstTexture);

            _gpu.Renderer.RenderTarget.Copy(
                srcKey,
                dstKey,
                0,
                0,
                srcWidth,
                srcHeight,
                0,
                0,
                dstWidth,
                dstHeight);
        }

        private static GalMemoryLayout GetLayout(bool linear)
        {
            return linear
                ? GalMemoryLayout.Pitch
                : GalMemoryLayout.BlockLinear;
        }

        private long MakeInt64From2XInt32(NvGpuEngine2DReg reg)
        {
            return
                (long)Registers[(int)reg + 0] << 32 |
                (uint)Registers[(int)reg + 1];
        }

        private void WriteRegister(NvGpuPbEntry pbEntry)
        {
            int argsCount = pbEntry.Arguments.Count;

            if (argsCount > 0)
            {
                Registers[pbEntry.Method] = pbEntry.Arguments[argsCount - 1];
            }
        }

        private int ReadRegister(NvGpuEngine2DReg reg)
        {
            return Registers[(int)reg];
        }

        private void WriteRegister(NvGpuEngine2DReg reg, int value)
        {
            Registers[(int)reg] = value;
        }
    }
}