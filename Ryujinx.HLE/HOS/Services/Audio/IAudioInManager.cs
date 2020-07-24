using Ryujinx.Cpu;
using System;
using System.Text;

namespace Ryujinx.HLE.HOS.Services.Audio
{
    [Service("audin:u")]
    class IAudioInManager : IpcService
    {
        private const string DefaultAudioInsName       = "BuiltInHeadset";
        private const uint   DefaultAudioInsNameLength = 0x100;

        public IAudioInManager(ServiceCtx context) { }

        [Command(0)]
        // ListAudioIns() -> (u32 count, buffer<bytes, 6> names)
        public ResultCode ListAudioIns(ServiceCtx context)
        {
            long bufferPosition = context.Request.ReceiveBuff[0].Position;
            long bufferSize     = context.Request.ReceiveBuff[0].Size;

            // NOTE: The service check if AudioInManager thread is started, if not it starts it.

            uint count = ListAudioInsImpl(context.Memory, bufferPosition, bufferSize, false);

            context.ResponseData.Write(count);

            return ResultCode.Success;
        }

        [Command(3)] // 3.0.0+
        // ListAudioInsAuto() -> (u32 count, buffer<bytes, 0x22> names)
        public ResultCode ListAudioInsAuto(ServiceCtx context)
        {
            (long bufferPosition, long bufferSize) = context.Request.GetBufferType0x22();

            // NOTE: The service check if AudioInManager thread is started, if not it starts it.

            uint count = ListAudioInsImpl(context.Memory, bufferPosition, bufferSize, false);

            context.ResponseData.Write(count);

            return ResultCode.Success;
        }

        [Command(4)] // 3.0.0+
        // ListAudioInsAutoFiltered() -> (u32 count, buffer<bytes, 0x22> names)
        public ResultCode ListAudioInsAutoFiltered(ServiceCtx context)
        {
            (long bufferPosition, long bufferSize) = context.Request.GetBufferType0x22();

            // NOTE: The service check if AudioInManager thread is started, if not it starts it.

            uint count = ListAudioInsImpl(context.Memory, bufferPosition, bufferSize, true);

            context.ResponseData.Write(count);

            return ResultCode.Success;
        }

        private uint ListAudioInsImpl(MemoryManager memory, long bufferPosition, long bufferSize, bool filtered = false)
        {
            uint deviceNameSize = 0;

            MemoryHelper.FillWithZeros(memory, bufferPosition, (int)bufferSize);

            if (bufferSize > 0)
            {
                if (!filtered)
                {
                    byte[] deviceNameBuffer = Encoding.ASCII.GetBytes(DefaultAudioInsName + "\0");

                    Array.Resize(ref deviceNameBuffer, (int)DefaultAudioInsNameLength);

                    memory.Write((ulong)bufferPosition, deviceNameBuffer);

                    deviceNameSize = DefaultAudioInsNameLength;
                }

                // NOTE: The service adds other input devices names available in the buffer,
                //       since we don't support it for now, it's fine to do nothing here.
            }

            return deviceNameSize;
        }
    }
}