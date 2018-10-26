using Ryujinx.Graphics.Memory;
using Ryujinx.Graphics.Texture;
using System.Collections.Generic;

namespace Ryujinx.Graphics
{
    public class NvGpuEngineM2mf : INvGpuEngine
    {
        public int[] Registers { get; private set; }

        private NvGpu _gpu;

        private Dictionary<int, NvGpuMethod> _methods;

        public NvGpuEngineM2mf(NvGpu gpu)
        {
            _gpu = gpu;

            Registers = new int[0x1d6];

            _methods = new Dictionary<int, NvGpuMethod>();

            void AddMethod(int meth, int count, int stride, NvGpuMethod method)
            {
                while (count-- > 0)
                {
                    _methods.Add(meth, method);

                    meth += stride;
                }
            }

            AddMethod(0xc0, 1, 1, Execute);
        }

        public void CallMethod(NvGpuVmm vmm, NvGpuPBEntry pbEntry)
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

        private void Execute(NvGpuVmm vmm, NvGpuPBEntry pbEntry)
        {
            //TODO: Some registers and copy modes are still not implemented.
            int control = pbEntry.Arguments[0];

            bool srcLinear = ((control >> 7) & 1) != 0;
            bool dstLinear = ((control >> 8) & 1) != 0;
            bool copy2D    = ((control >> 9) & 1) != 0;

            long srcAddress = MakeInt64From2XInt32(NvGpuEngineM2mfReg.SrcAddress);
            long dstAddress = MakeInt64From2XInt32(NvGpuEngineM2mfReg.DstAddress);

            int srcPitch = ReadRegister(NvGpuEngineM2mfReg.SrcPitch);
            int dstPitch = ReadRegister(NvGpuEngineM2mfReg.DstPitch);

            int xCount = ReadRegister(NvGpuEngineM2mfReg.XCount);
            int yCount = ReadRegister(NvGpuEngineM2mfReg.YCount);

            int swizzle = ReadRegister(NvGpuEngineM2mfReg.Swizzle);

            int dstBlkDim = ReadRegister(NvGpuEngineM2mfReg.DstBlkDim);
            int dstSizeX  = ReadRegister(NvGpuEngineM2mfReg.DstSizeX);
            int dstSizeY  = ReadRegister(NvGpuEngineM2mfReg.DstSizeY);
            int dstSizeZ  = ReadRegister(NvGpuEngineM2mfReg.DstSizeZ);
            int dstPosXy  = ReadRegister(NvGpuEngineM2mfReg.DstPosXy);
            int dstPosZ   = ReadRegister(NvGpuEngineM2mfReg.DstPosZ);

            int srcBlkDim = ReadRegister(NvGpuEngineM2mfReg.SrcBlkDim);
            int srcSizeX  = ReadRegister(NvGpuEngineM2mfReg.SrcSizeX);
            int srcSizeY  = ReadRegister(NvGpuEngineM2mfReg.SrcSizeY);
            int srcSizeZ  = ReadRegister(NvGpuEngineM2mfReg.SrcSizeZ);
            int srcPosXy  = ReadRegister(NvGpuEngineM2mfReg.SrcPosXy);
            int srcPosZ   = ReadRegister(NvGpuEngineM2mfReg.SrcPosZ);

            int srcCpp = ((swizzle >> 20) & 7) + 1;
            int dstCpp = ((swizzle >> 24) & 7) + 1;

            int dstPosX = (dstPosXy >>  0) & 0xffff;
            int dstPosY = (dstPosXy >> 16) & 0xffff;

            int srcPosX = (srcPosXy >>  0) & 0xffff;
            int srcPosY = (srcPosXy >> 16) & 0xffff;

            int srcBlockHeight = 1 << ((srcBlkDim >> 4) & 0xf);
            int dstBlockHeight = 1 << ((dstBlkDim >> 4) & 0xf);

            long srcPa = vmm.GetPhysicalAddress(srcAddress);
            long dstPa = vmm.GetPhysicalAddress(dstAddress);

            if (copy2D)
            {
                if (srcLinear)
                {
                    srcPosX = srcPosY = srcPosZ = 0;
                }

                if (dstLinear)
                {
                    dstPosX = dstPosY = dstPosZ = 0;
                }

                if (srcLinear && dstLinear)
                {
                    for (int y = 0; y < yCount; y++)
                    {
                        int srcOffset = (srcPosY + y) * srcPitch + srcPosX * srcCpp;
                        int dstOffset = (dstPosY + y) * dstPitch + dstPosX * dstCpp;

                        long src = srcPa + (uint)srcOffset;
                        long dst = dstPa + (uint)dstOffset;

                        vmm.Memory.CopyBytes(src, dst, xCount * srcCpp);
                    }
                }
                else
                {
                    ISwizzle srcSwizzle;

                    if (srcLinear)
                    {
                        srcSwizzle = new LinearSwizzle(srcPitch, srcCpp);
                    }
                    else
                    {
                        srcSwizzle = new BlockLinearSwizzle(srcSizeX, srcCpp, srcBlockHeight);
                    }

                    ISwizzle dstSwizzle;

                    if (dstLinear)
                    {
                        dstSwizzle = new LinearSwizzle(dstPitch, dstCpp);
                    }
                    else
                    {
                        dstSwizzle = new BlockLinearSwizzle(dstSizeX, dstCpp, dstBlockHeight);
                    }

                    for (int y = 0; y < yCount; y++)
                    {
                        for (int x = 0; x < xCount; x++)
                    {
                        int srcOffset = srcSwizzle.GetSwizzleOffset(srcPosX + x, srcPosY + y);
                        int dstOffset = dstSwizzle.GetSwizzleOffset(dstPosX + x, dstPosY + y);

                        long src = srcPa + (uint)srcOffset;
                        long dst = dstPa + (uint)dstOffset;

                        vmm.Memory.CopyBytes(src, dst, srcCpp);
                    }
                    }
                }
            }
            else
            {
                vmm.Memory.CopyBytes(srcPa, dstPa, xCount);
            }
        }

        private long MakeInt64From2XInt32(NvGpuEngineM2mfReg reg)
        {
            return
                ((long)Registers[(int)reg + 0] << 32) |
                (uint)Registers[(int)reg + 1];
        }

        private void WriteRegister(NvGpuPBEntry pbEntry)
        {
            int argsCount = pbEntry.Arguments.Count;

            if (argsCount > 0)
            {
                Registers[pbEntry.Method] = pbEntry.Arguments[argsCount - 1];
            }
        }

        private int ReadRegister(NvGpuEngineM2mfReg reg)
        {
            return Registers[(int)reg];
        }

        private void WriteRegister(NvGpuEngineM2mfReg reg, int value)
        {
            Registers[(int)reg] = value;
        }
    }
}