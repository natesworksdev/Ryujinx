using ChocolArm64.Memory;
using Ryujinx.Common.Logging;
using Ryujinx.Graphics.Memory;
using Ryujinx.HLE.HOS.Services.Nv.NvGpuAS;
using System;
using System.Collections.Concurrent;

namespace Ryujinx.HLE.HOS.Services.Nv.NvHostChannel
{
    class NvHostChannelIoctl
    {
        private class ChannelsPerProcess
        {
            public ConcurrentDictionary<NvChannelName, NvChannel> Channels { get; private set; }

            public ChannelsPerProcess()
            {
                Channels = new ConcurrentDictionary<NvChannelName, NvChannel>();

                Channels.TryAdd(NvChannelName.Gpu, new NvChannel());
            }
        }

        private static ConcurrentDictionary<Process, ChannelsPerProcess> _channels;

        static NvHostChannelIoctl()
        {
            _channels = new ConcurrentDictionary<Process, ChannelsPerProcess>();
        }

        public static int ProcessIoctlGpu(ServiceCtx context, int cmd)
        {
            return ProcessIoctl(context, NvChannelName.Gpu, cmd);
        }

        public static int ProcessIoctl(ServiceCtx context, NvChannelName channel, int cmd)
        {
            switch (cmd & 0xffff)
            {
                case 0x4714: return SetUserData      (context);
                case 0x4801: return SetNvMap         (context);
                case 0x4803: return SetTimeout       (context, channel);
                case 0x4808: return SubmitGpfifo     (context);
                case 0x4809: return AllocObjCtx      (context);
                case 0x480b: return ZcullBind        (context);
                case 0x480c: return SetErrorNotifier (context);
                case 0x480d: return SetPriority      (context);
                case 0x481a: return AllocGpfifoEx2   (context);
                case 0x481b: return KickoffPbWithAttr(context);
            }

            throw new NotImplementedException(cmd.ToString("x8"));
        }

        private static int SetUserData(ServiceCtx context)
        {
            long inputPosition  = context.Request.GetBufferType0X21().Position;
            long outputPosition = context.Request.GetBufferType0X22().Position;

            Logger.PrintStub(LogClass.ServiceNv, "Stubbed.");

            return NvResult.Success;
        }

        private static int SetNvMap(ServiceCtx context)
        {
            long inputPosition  = context.Request.GetBufferType0X21().Position;
            long outputPosition = context.Request.GetBufferType0X22().Position;

            Logger.PrintStub(LogClass.ServiceNv, "Stubbed.");

            return NvResult.Success;
        }

        private static int SetTimeout(ServiceCtx context, NvChannelName channel)
        {
            long inputPosition = context.Request.GetBufferType0X21().Position;

            GetChannel(context, channel).Timeout = context.Memory.ReadInt32(inputPosition);

            return NvResult.Success;
        }

        private static int SubmitGpfifo(ServiceCtx context)
        {
            long inputPosition  = context.Request.GetBufferType0X21().Position;
            long outputPosition = context.Request.GetBufferType0X22().Position;

            NvHostChannelSubmitGpfifo args = AMemoryHelper.Read<NvHostChannelSubmitGpfifo>(context.Memory, inputPosition);

            NvGpuVmm vmm = NvGpuAsIoctl.GetAsCtx(context).Vmm;;

            for (int index = 0; index < args.NumEntries; index++)
            {
                long gpfifo = context.Memory.ReadInt64(inputPosition + 0x18 + index * 8);

                PushGpfifo(context, vmm, gpfifo);
            }

            args.SyncptId    = 0;
            args.SyncptValue = 0;

            AMemoryHelper.Write(context.Memory, outputPosition, args);

            return NvResult.Success;
        }

        private static int AllocObjCtx(ServiceCtx context)
        {
            long inputPosition  = context.Request.GetBufferType0X21().Position;
            long outputPosition = context.Request.GetBufferType0X22().Position;

            Logger.PrintStub(LogClass.ServiceNv, "Stubbed.");

            return NvResult.Success;
        }

        private static int ZcullBind(ServiceCtx context)
        {
            long inputPosition  = context.Request.GetBufferType0X21().Position;
            long outputPosition = context.Request.GetBufferType0X22().Position;

            Logger.PrintStub(LogClass.ServiceNv, "Stubbed.");

            return NvResult.Success;
        }

        private static int SetErrorNotifier(ServiceCtx context)
        {
            long inputPosition  = context.Request.GetBufferType0X21().Position;
            long outputPosition = context.Request.GetBufferType0X22().Position;

            Logger.PrintStub(LogClass.ServiceNv, "Stubbed.");

            return NvResult.Success;
        }

        private static int SetPriority(ServiceCtx context)
        {
            long inputPosition  = context.Request.GetBufferType0X21().Position;
            long outputPosition = context.Request.GetBufferType0X22().Position;

            Logger.PrintStub(LogClass.ServiceNv, "Stubbed.");

            return NvResult.Success;
        }

        private static int AllocGpfifoEx2(ServiceCtx context)
        {
            long inputPosition  = context.Request.GetBufferType0X21().Position;
            long outputPosition = context.Request.GetBufferType0X22().Position;

            Logger.PrintStub(LogClass.ServiceNv, "Stubbed.");

            return NvResult.Success;
        }

        private static int KickoffPbWithAttr(ServiceCtx context)
        {
            long inputPosition  = context.Request.GetBufferType0X21().Position;
            long outputPosition = context.Request.GetBufferType0X22().Position;

            NvHostChannelSubmitGpfifo args = AMemoryHelper.Read<NvHostChannelSubmitGpfifo>(context.Memory, inputPosition);

            NvGpuVmm vmm = NvGpuAsIoctl.GetAsCtx(context).Vmm;;

            for (int index = 0; index < args.NumEntries; index++)
            {
                long gpfifo = context.Memory.ReadInt64(args.Address + index * 8);

                PushGpfifo(context, vmm, gpfifo);
            }

            args.SyncptId    = 0;
            args.SyncptValue = 0;

            AMemoryHelper.Write(context.Memory, outputPosition, args);

            return NvResult.Success;
        }

        private static void PushGpfifo(ServiceCtx context, NvGpuVmm vmm, long gpfifo)
        {
            long va = gpfifo & 0xff_ffff_ffff;

            int size = (int)(gpfifo >> 40) & 0x7ffffc;

            byte[] data = vmm.ReadBytes(va, size);

            NvGpuPbEntry[] pushBuffer = NvGpuPushBuffer.Decode(data);

            context.Device.Gpu.Fifo.PushBuffer(vmm, pushBuffer);
        }

        public static NvChannel GetChannel(ServiceCtx context, NvChannelName channel)
        {
            ChannelsPerProcess cpp = _channels.GetOrAdd(context.Process, (key) =>
            {
                return new ChannelsPerProcess();
            });

            return cpp.Channels[channel];
        }

        public static void UnloadProcess(Process process)
        {
            _channels.TryRemove(process, out _);
        }
    }
}