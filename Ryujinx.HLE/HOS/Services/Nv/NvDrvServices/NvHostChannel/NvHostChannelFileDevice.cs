using Ryujinx.HLE.HOS.Kernel.Process;
using System;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostChannel
{
    class NvHostChannelFileDevice : NvFileDevice
    {
        public NvHostChannelFileDevice(KProcess owner) : base(owner)
        {

        }

        public override NvInternalResult Ioctl(NvIoctl command, Span<byte> arguments)
        {
            NvInternalResult result = NvInternalResult.NotImplemented;

            if (command.GetTypeValue() == NvIoctl.NvHostMagic)
            {
                switch (command.GetNumberValue())
                {

                }
            }
            else if (command.GetTypeValue() == NvIoctl.NvGpuMagic)
            {
                switch (command.GetNumberValue())
                {

                }
            }

            return result;
        }

        public override void Close()
        {
            throw new NotImplementedException();
        }
    }
}
