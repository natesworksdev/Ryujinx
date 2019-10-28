using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrlGpu.Types;
using System;
using System.Diagnostics;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrlGpu
{
    class NvHostCtrlGpuFileDevice : NvFileDevice
    {
        private static Stopwatch _pTimer    = new Stopwatch();
        private static double    _ticksToNs = (1.0 / Stopwatch.Frequency) * 1_000_000_000;

        public NvHostCtrlGpuFileDevice(KProcess owner) : base(owner)
        {
        }

        static NvHostCtrlGpuFileDevice()
        {
            _pTimer.Start();
        }

        public override NvInternalResult Ioctl(NvIoctl command, Span<byte> arguments)
        {
            NvInternalResult result = NvInternalResult.NotImplemented;

            switch (command.GetNumberValue())
            {
                case 0x1:
                    result = CallIoctlMethod<ZcullGetCtxSizeArguments>(ZcullGetCtxSize, arguments);
                    break;
                case 0x2:
                    result = CallIoctlMethod<ZcullGetInfoArguments>(ZcullGetInfo, arguments);
                    break;
                case 0x3:
                    result = CallIoctlMethod<ZbcSetTableArguments>(ZbcSetTable, arguments);
                    break;
                case 0x5:
                    result = CallIoctlMethod<GetCharacteristicsArguments>(GetCharacteristics, arguments);
                    break;
                case 0x6:
                    result = CallIoctlMethod<GetTpcMasksArguments>(GetTpcMasks, arguments);
                    break;
                case 0x14:
                    result = CallIoctlMethod<GetActiveSlotMaskArguments>(GetActiveSlotMask, arguments);
                    break;
                case 0x1c:
                    result = CallIoctlMethod<GetGpuTimeArguments>(GetGpuTime, arguments);
                    break;
            }

            return result;
        }

        public override void Close()
        {
            // TODO
        }

        private NvInternalResult ZcullGetCtxSize(ref ZcullGetCtxSizeArguments arguments)
        {
            arguments.Size = 1;

            return NvInternalResult.Success;
        }

        private NvInternalResult ZcullGetInfo(ref ZcullGetInfoArguments arguments)
        {
            arguments.WidthAlignPixels           = 0x20;
            arguments.HeightAlignPixels          = 0x20;
            arguments.PixelSquaresByAliquots     = 0x400;
            arguments.AliquotTotal               = 0x800;
            arguments.RegionByteMultiplier       = 0x20;
            arguments.RegionHeaderSize           = 0x20;
            arguments.SubregionHeaderSize        = 0xc0;
            arguments.SubregionWidthAlignPixels  = 0x20;
            arguments.SubregionHeightAlignPixels = 0x40;
            arguments.SubregionCount             = 0x10;

            return NvInternalResult.Success;
        }

        private NvInternalResult ZbcSetTable(ref ZbcSetTableArguments arguments)
        {
            Logger.PrintStub(LogClass.ServiceNv);

            return NvInternalResult.Success;
        }

        private NvInternalResult GetCharacteristics(ref GetCharacteristicsArguments arguments)
        {
            arguments.BufferSize = 0xa0;

            arguments.Characteristics.Arch                   = 0x120;
            arguments.Characteristics.Impl                   = 0xb;
            arguments.Characteristics.Rev                    = 0xa1;
            arguments.Characteristics.NumGpc                 = 0x1;
            arguments.Characteristics.L2CacheSize            = 0x40000;
            arguments.Characteristics.OnBoardVideoMemorySize = 0x0;
            arguments.Characteristics.NumTpcPerGpc           = 0x2;
            arguments.Characteristics.BusType                = 0x20;
            arguments.Characteristics.BigPageSize            = 0x20000;
            arguments.Characteristics.CompressionPageSize    = 0x20000;
            arguments.Characteristics.PdeCoverageBitCount    = 0x1b;
            arguments.Characteristics.AvailableBigPageSizes  = 0x30000;
            arguments.Characteristics.GpcMask                = 0x1;
            arguments.Characteristics.SmArchSmVersion        = 0x503;
            arguments.Characteristics.SmArchSpaVersion       = 0x503;
            arguments.Characteristics.SmArchWarpCount        = 0x80;
            arguments.Characteristics.GpuVaBitCount          = 0x28;
            arguments.Characteristics.Reserved               = 0x0;
            arguments.Characteristics.Flags                  = 0x55;
            arguments.Characteristics.TwodClass              = 0x902d;
            arguments.Characteristics.ThreedClass            = 0xb197;
            arguments.Characteristics.ComputeClass           = 0xb1c0;
            arguments.Characteristics.GpfifoClass            = 0xb06f;
            arguments.Characteristics.InlineToMemoryClass    = 0xa140;
            arguments.Characteristics.DmaCopyClass           = 0xb0b5;
            arguments.Characteristics.MaxFbpsCount           = 0x1;
            arguments.Characteristics.FbpEnMask              = 0x0;
            arguments.Characteristics.MaxLtcPerFbp           = 0x2;
            arguments.Characteristics.MaxLtsPerLtc           = 0x1;
            arguments.Characteristics.MaxTexPerTpc           = 0x0;
            arguments.Characteristics.MaxGpcCount            = 0x1;
            arguments.Characteristics.RopL2EnMask0           = 0x21d70;
            arguments.Characteristics.RopL2EnMask1           = 0x0;
            arguments.Characteristics.ChipName               = 0x6230326d67;
            arguments.Characteristics.GrCompbitStoreBaseHw   = 0x0;

            return NvInternalResult.Success;
        }

        private NvInternalResult GetTpcMasks(ref GetTpcMasksArguments arguments)
        {
            if (arguments.MaskBufferSize != 0)
            {
                arguments.TpcMask = 3;
            }

            return NvInternalResult.Success;
        }

        private NvInternalResult GetActiveSlotMask(ref GetActiveSlotMaskArguments arguments)
        {
            Logger.PrintStub(LogClass.ServiceNv);

            arguments.Slot = 0x07;
            arguments.Mask = 0x01;

            return NvInternalResult.Success;
        }

        private NvInternalResult GetGpuTime(ref GetGpuTimeArguments arguments)
        {
            arguments.Timestamp = GetPTimerNanoSeconds();

            return NvInternalResult.Success;
        }

        private static ulong GetPTimerNanoSeconds()
        {
            double ticks = _pTimer.ElapsedTicks;

            return (ulong)(ticks * _ticksToNs) & 0xff_ffff_ffff_ffff;
        }
    }
}
