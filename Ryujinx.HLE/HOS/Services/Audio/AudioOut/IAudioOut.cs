using Ryujinx.Audio.Common;
using Ryujinx.HLE.HOS.Kernel.Threading;
using System;

namespace Ryujinx.HLE.HOS.Services.Audio.AudioOut
{
    interface IAudioOut : IDisposable
    {
        public AudioDeviceState GetState();

        public ResultCode Start();

        public ResultCode Stop();

        public ResultCode AppendBuffer(ulong bufferTag, ref AudioUserBuffer buffer);

        public KEvent RegisterBufferEvent();

        public ResultCode GetReleasedBuffers(Span<ulong> releasedBuffers, out uint releasedCount);

        public bool ContainsBuffer(ulong bufferTag);

        public uint GetBufferCount();

        public ulong GetPlayedSampleCount();

        public bool FlushBuffers();

        public void SetVolume(float volume);

        public float GetVolume();
    }
}
