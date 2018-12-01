namespace Ryujinx.HLE.HOS.Kernel
{
    class KMemoryArrange
    {
        public KMemoryArrangeRegion Service     { get; private set; }
        public KMemoryArrangeRegion NvServices  { get; private set; }
        public KMemoryArrangeRegion Applet      { get; private set; }
        public KMemoryArrangeRegion Application { get; private set; }

        public KMemoryArrange(
            KMemoryArrangeRegion service,
            KMemoryArrangeRegion nvServices,
            KMemoryArrangeRegion applet,
            KMemoryArrangeRegion application)
        {
            this.Service     = service;
            this.NvServices  = nvServices;
            this.Applet      = applet;
            this.Application = application;
        }
    }
}