using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Memory;
using Ryujinx.Graphics.Texture;

namespace Ryujinx.Graphics
{
    class NvGpuEngine2d : INvGpuEngine
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

        private NvGpu Gpu;

        public NvGpuEngine2d(NvGpu Gpu)
        {
            this.Gpu = Gpu;

            Registers = new int[0x238];
        }

        public void CallMethod(NvGpuVmm Vmm, GpuMethodCall MethCall)
        {
            WriteRegister(MethCall);

            if ((NvGpuEngine2dReg)MethCall.Method == NvGpuEngine2dReg.BlitSrcYInt)
            {
                TextureCopy(Vmm);
            }
        }

        private void TextureCopy(NvGpuVmm Vmm)
        {
            CopyOperation Operation = (CopyOperation)ReadRegister(NvGpuEngine2dReg.CopyOperation);

            int  DstFormat = ReadRegister(NvGpuEngine2dReg.DstFormat);
            bool DstLinear = ReadRegister(NvGpuEngine2dReg.DstLinear) != 0;
            int  DstWidth  = ReadRegister(NvGpuEngine2dReg.DstWidth);
            int  DstHeight = ReadRegister(NvGpuEngine2dReg.DstHeight);
            int  DstPitch  = ReadRegister(NvGpuEngine2dReg.DstPitch);
            int  DstBlkDim = ReadRegister(NvGpuEngine2dReg.DstBlockDimensions);

            int  SrcFormat = ReadRegister(NvGpuEngine2dReg.SrcFormat);
            bool SrcLinear = ReadRegister(NvGpuEngine2dReg.SrcLinear) != 0;
            int  SrcWidth  = ReadRegister(NvGpuEngine2dReg.SrcWidth);
            int  SrcHeight = ReadRegister(NvGpuEngine2dReg.SrcHeight);
            int  SrcPitch  = ReadRegister(NvGpuEngine2dReg.SrcPitch);
            int  SrcBlkDim = ReadRegister(NvGpuEngine2dReg.SrcBlockDimensions);

            int DstBlitX = ReadRegister(NvGpuEngine2dReg.BlitDstX);
            int DstBlitY = ReadRegister(NvGpuEngine2dReg.BlitDstY);
            int DstBlitW = ReadRegister(NvGpuEngine2dReg.BlitDstW);
            int DstBlitH = ReadRegister(NvGpuEngine2dReg.BlitDstH);

            double BlitDuDx = ReadRegisterFixed1_31_32(NvGpuEngine2dReg.BlitDuDxFract);
            double BlitDvDy = ReadRegisterFixed1_31_32(NvGpuEngine2dReg.BlitDvDyFract);

            double SrcBlitX = ReadRegisterFixed1_31_32(NvGpuEngine2dReg.BlitSrcXFract);
            double SrcBlitY = ReadRegisterFixed1_31_32(NvGpuEngine2dReg.BlitSrcYFract);

            GalImageFormat SrcImgFormat = ImageUtils.ConvertSurface((GalSurfaceFormat)SrcFormat);
            GalImageFormat DstImgFormat = ImageUtils.ConvertSurface((GalSurfaceFormat)DstFormat);

            GalMemoryLayout SrcLayout = GetLayout(SrcLinear);
            GalMemoryLayout DstLayout = GetLayout(DstLinear);

            int SrcBlockHeight = 1 << ((SrcBlkDim >> 4) & 0xf);
            int DstBlockHeight = 1 << ((DstBlkDim >> 4) & 0xf);

            long SrcAddress = MakeInt64From2xInt32(NvGpuEngine2dReg.SrcAddress);
            long DstAddress = MakeInt64From2xInt32(NvGpuEngine2dReg.DstAddress);

            long SrcKey = Vmm.GetPhysicalAddress(SrcAddress);
            long DstKey = Vmm.GetPhysicalAddress(DstAddress);

            GalImage SrcTexture = new GalImage(
                SrcWidth,
                SrcHeight, 1,
                SrcBlockHeight,
                SrcLayout,
                SrcImgFormat);

            GalImage DstTexture = new GalImage(
                DstWidth,
                DstHeight, 1,
                DstBlockHeight,
                DstLayout,
                DstImgFormat);

            SrcTexture.Pitch = SrcPitch;
            DstTexture.Pitch = DstPitch;

            Gpu.ResourceManager.SendTexture(Vmm, SrcKey, SrcTexture);
            Gpu.ResourceManager.SendTexture(Vmm, DstKey, DstTexture);

            int SrcBlitX1 = (int)SrcBlitX;
            int SrcBlitY1 = (int)SrcBlitY;

            int SrcBlitX2 = (int)(SrcBlitX + DstBlitW * BlitDuDx);
            int SrcBlitY2 = (int)(SrcBlitY + DstBlitH * BlitDvDy);

            Gpu.Renderer.RenderTarget.Copy(
                SrcKey,
                DstKey,
                SrcBlitX1,
                SrcBlitY1,
                SrcBlitX2,
                SrcBlitY2,
                DstBlitX,
                DstBlitY,
                DstBlitX + DstBlitW,
                DstBlitY + DstBlitH);

            //Do a guest side copy aswell. This is necessary when
            //the texture is modified by the guest, however it doesn't
            //work when resources that the gpu can write to are copied,
            //like framebuffers.
            ImageUtils.CopyTexture(
                Vmm,
                SrcTexture,
                DstTexture,
                SrcAddress,
                DstAddress,
                SrcBlitX1,
                SrcBlitY1,
                DstBlitX,
                DstBlitY,
                DstBlitW,
                DstBlitH);

            Vmm.IsRegionModified(DstKey, ImageUtils.GetSize(DstTexture), NvGpuBufferType.Texture);
        }

        private static GalMemoryLayout GetLayout(bool Linear)
        {
            return Linear
                ? GalMemoryLayout.Pitch
                : GalMemoryLayout.BlockLinear;
        }

        private long MakeInt64From2xInt32(NvGpuEngine2dReg Reg)
        {
            return
                (long)Registers[(int)Reg + 0] << 32 |
                (uint)Registers[(int)Reg + 1];
        }

        private void WriteRegister(GpuMethodCall MethCall)
        {
            Registers[MethCall.Method] = MethCall.Argument;
        }

        private double ReadRegisterFixed1_31_32(NvGpuEngine2dReg Reg)
        {
            long Low  = (uint)ReadRegister(Reg + 0);
            long High = (uint)ReadRegister(Reg + 1);

            long Value = Low | (High << 32);

            return Value * (1d / 0x100000000);
        }

        private int ReadRegister(NvGpuEngine2dReg Reg)
        {
            return Registers[(int)Reg];
        }
    }
}