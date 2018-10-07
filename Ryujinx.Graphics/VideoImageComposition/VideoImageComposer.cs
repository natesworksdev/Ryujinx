using Ryujinx.Graphics.Memory;

namespace Ryujinx.Graphics.VideoImageComposition
{
    class VideoImageComposer
    {
        private NvGpu Gpu;

        private long LumaPlaneAddress;
        private long ChromaPlaneAddress;

        public VideoImageComposer(NvGpu Gpu)
        {
            this.Gpu = Gpu;
        }

        public void Process(NvGpuVmm Vmm, int MethodOffset, int[] Arguments)
        {
            VideoImageComposerMeth Method = (VideoImageComposerMeth)MethodOffset;

            switch (Method)
            {
                case VideoImageComposerMeth.Execute:                Execute               (Vmm, Arguments); break;
                case VideoImageComposerMeth.SetVDecLumaPlaneAddr:   SetVDecLumaPlaneAddr  (Vmm, Arguments); break;
                case VideoImageComposerMeth.SetVDecChromaPlaneAddr: SetVDecChromaPlaneAddr(Vmm, Arguments); break;
            }
        }

        private void Execute(NvGpuVmm Vmm, int[] Arguments)
        {
            Gpu.VideoDecoder.CopyPlanes(Vmm, LumaPlaneAddress, ChromaPlaneAddress);
        }

        private void SetVDecLumaPlaneAddr(NvGpuVmm Vmm, int[] Arguments)
        {
            LumaPlaneAddress = GetAddress(Arguments);
        }

        private void SetVDecChromaPlaneAddr(NvGpuVmm Vmm, int[] Arguments)
        {
            ChromaPlaneAddress = GetAddress(Arguments);
        }

        private static long GetAddress(int[] Arguments)
        {
            return (long)(uint)Arguments[0] << 8;
        }
    }
}