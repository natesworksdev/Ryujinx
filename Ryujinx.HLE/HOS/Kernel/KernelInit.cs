namespace Ryujinx.HLE.HOS.Kernel
{
    static class KernelInit
    {
        public static KMemoryRegionManager[] GetMemoryRegions()
        {
            KMemoryArrange Arrange = GetMemoryArrange();

            return new KMemoryRegionManager[]
            {
                GetMemoryRegion(Arrange.Application),
                GetMemoryRegion(Arrange.Applet),
                GetMemoryRegion(Arrange.Service),
                GetMemoryRegion(Arrange.NvServices)
            };
        }

        private static KMemoryRegionManager GetMemoryRegion(KMemoryArrangeRegion Region)
        {
            return new KMemoryRegionManager(
                Region.Address,
                Region.Size,
                Region.EndAddr);
        }

        private static KMemoryArrange GetMemoryArrange()
        {
            int McEmemCfg = 0x1000;

            ulong EmemApertureSize = (ulong)(McEmemCfg & 0x3fff) << 20;

            int KernelMemoryCfg = 0;

            ulong RamSize;

            switch ((KernelMemoryCfg >> 16) & 3)
            {
                case 1:  RamSize = 0x180000000; break;
                case 2:  RamSize = 0x200000000; break;
                default: RamSize = 0x100000000; break;
            }

            long RamPart0;
            long RamPart1;

            if (RamSize * 2 > EmemApertureSize)
            {
                RamPart0 = (long)(EmemApertureSize / 2);
                RamPart1 = (long)(EmemApertureSize / 2);
            }
            else
            {
                RamPart0 = (long)EmemApertureSize;
                RamPart1 = 0;
            }

            int MemoryArrange = 1;

            long ApplicationRgSize;

            switch (MemoryArrange)
            {
                case 2:    ApplicationRgSize = 0x80000000;  break;
                case 0x11:
                case 0x21: ApplicationRgSize = 0x133400000; break;
                default:   ApplicationRgSize = 0xcd500000;  break;
            }

            long AppletRgSize;

            switch (MemoryArrange)
            {
                case 2:    AppletRgSize = 0x61200000; break;
                case 3:    AppletRgSize = 0x1c000000; break;
                case 0x11: AppletRgSize = 0x23200000; break;
                case 0x12:
                case 0x21: AppletRgSize = 0x89100000; break;
                default:   AppletRgSize = 0x1fb00000; break;
            }

            KMemoryArrangeRegion ServiceRg;
            KMemoryArrangeRegion NvServicesRg;
            KMemoryArrangeRegion AppletRg;
            KMemoryArrangeRegion ApplicationRg;

            const long NvServicesRgSize = 0x29ba000;

            long ApplicationRgEnd = DramMemoryMap.DramEnd; //- RamPart0;

            ApplicationRg = new KMemoryArrangeRegion(ApplicationRgEnd - ApplicationRgSize, ApplicationRgSize);

            long NvServicesRgEnd = ApplicationRg.Address - AppletRgSize;

            NvServicesRg = new KMemoryArrangeRegion(NvServicesRgEnd - NvServicesRgSize, NvServicesRgSize);
            AppletRg     = new KMemoryArrangeRegion(NvServicesRgEnd, AppletRgSize);

            //Note: There is an extra region used by the kernel, however
            //since we are doing HLE we are not going to use that memory, so give all
            //the remaining memory space to services.
            long ServiceRgSize = NvServicesRg.Address - DramMemoryMap.KernelReserveBase;

            ServiceRg = new KMemoryArrangeRegion(DramMemoryMap.KernelReserveBase, ServiceRgSize);

            return new KMemoryArrange(ServiceRg, NvServicesRg, AppletRg, ApplicationRg);
        }
    }
}