using Ryujinx.Audio.Common;
using Ryujinx.HLE.HOS.Kernel.Threading;
using System;

namespace Ryujinx.HLE.HOS.Services.Audio.AudioIn
{
    interface IAudioIn : IDisposable
    {
        public AudioDeviceState GetState();

        public ResultCode Start();

        public ResultCode Stop();

        public ResultCode AppendBuffer(ulong bufferTag, ref AudioUserBuffer buffer);

        // NOTE: This is broken by design... not quite sure what it's used for (if anything in production).
        public ResultCode AppendBufferBroken(ulong bufferTag, ref AudioUserBuffer buffer, uint handle);

        public KEvent RegisterBufferEvent();

        public ResultCode GetReleasedBuffer(Span<ulong> releasedBuffers, out uint releasedCount);

        public bool ContainsBuffer(ulong bufferTag);

        public uint GetBufferCount();

        public bool FlushBuffers();

        public void SetVolume(float volume);

        public float GetVolume();
    }
}
