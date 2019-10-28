using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostChannel.Types;
using System;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostChannel
{
    internal class NvHostGpuFileDevice : NvHostChannelFileDevice
    {
        public NvHostGpuFileDevice(ServiceCtx context) : base(context)
        {
        }

        public override NvInternalResult Ioctl2(NvIoctl command, Span<byte> arguments, Span<byte> inlineInBuffer)
        {
            NvInternalResult result = NvInternalResult.NotImplemented;

            if (command.GetTypeValue() == NvIoctl.NvHostMagic)
            {
                switch (command.GetNumberValue())
                {
                    case 0x1b:
                        result = CallIoctlMethod<SubmitGpfifoArguments, long>(SubmitGpfifoEx, arguments, inlineInBuffer);
                        break;
                }
            }
            return result;
        }

        private NvInternalResult SubmitGpfifoEx(ref SubmitGpfifoArguments arguments, Span<long> inlineData)
        {
            return SubmitGpfifo(ref arguments, inlineData);
        }
    }
}
