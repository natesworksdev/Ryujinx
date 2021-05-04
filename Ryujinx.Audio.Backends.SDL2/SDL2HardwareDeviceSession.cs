using Ryujinx.Audio.Backends.Common;
using Ryujinx.Audio.Common;
using Ryujinx.Memory;
using System;
using System.Collections.Concurrent;
using System.Threading;

using static SDL2.SDL;

namespace Ryujinx.Audio.Backends.SDL2
{
    class SDL2HardwareDeviceSession : HardwareDeviceSessionOutputBase
    {
        private SDL2HardwareDeviceDriver _driver;
        private ConcurrentQueue<SDL2AudioBuffer> _queuedBuffers;
        private DynamicRingBuffer _ringBuffer;
        private ulong _playedSampleCount;
        private ManualResetEvent _updateRequiredEvent;
        private uint _outputStream;
        private SDL_AudioCallback _callbackDelegate;
        private int _bytesPerFrame;

        public SDL2HardwareDeviceSession(SDL2HardwareDeviceDriver driver, IVirtualMemoryManager memoryManager, SampleFormat requestedSampleFormat, uint requestedSampleRate, uint requestedChannelCount) : base(memoryManager, requestedSampleFormat, requestedSampleRate, requestedChannelCount)
        {
            _driver = driver;
            _updateRequiredEvent = _driver.GetUpdateRequiredEvent();
            _queuedBuffers = new ConcurrentQueue<SDL2AudioBuffer>();
            _ringBuffer = new DynamicRingBuffer();
            _callbackDelegate = Update;
            _bytesPerFrame = BackendHelper.GetSampleSize(RequestedSampleFormat) * (int)RequestedChannelCount;

            SetupOutputStream();
        }

        private void SetupOutputStream()
        {
            _outputStream = SDL2HardwareDeviceDriver.OpenStream(RequestedSampleFormat, RequestedSampleRate, RequestedChannelCount, Constants.TargetSampleCount, _callbackDelegate);
        }

        public override bool RegisterBuffer(AudioBuffer buffer, byte[] samples)
        {
            bool isValid = base.RegisterBuffer(buffer, samples);

            return isValid;
        }

        private unsafe void Update(IntPtr userdata, IntPtr stream, int streamLength)
        {
            Span<byte> streamSpan = new Span<byte>((void*)stream, streamLength);

            int maxFrameCount = (int)GetSampleCount(streamLength);
            int bufferedFrames = _ringBuffer.Length / _bytesPerFrame;

            int frameCount = Math.Min(bufferedFrames, maxFrameCount);

            if (frameCount == 0)
            {
                // SDL2 left the responsability to the user to clear the buffer.
                streamSpan.Fill(0);

                return;
            }

            byte[] samples = new byte[frameCount * _bytesPerFrame];

            _ringBuffer.Read(samples, 0, samples.Length);

            samples.AsSpan().CopyTo(streamSpan);
            streamSpan.Slice(samples.Length).Fill(0);

            ulong sampleCount = GetSampleCount(samples.Length);

            ulong availaibleSampleCount = sampleCount;

            bool needUpdate = false;

            while (availaibleSampleCount > 0 && _queuedBuffers.TryPeek(out SDL2AudioBuffer driverBuffer))
            {
                ulong sampleStillNeeded = driverBuffer.SampleCount - Interlocked.Read(ref driverBuffer.SamplePlayed);
                ulong playedAudioBufferSampleCount = Math.Min(sampleStillNeeded, availaibleSampleCount);

                Interlocked.Add(ref driverBuffer.SamplePlayed, playedAudioBufferSampleCount);
                availaibleSampleCount -= playedAudioBufferSampleCount;

                if (Interlocked.Read(ref driverBuffer.SamplePlayed) == driverBuffer.SampleCount)
                {
                    _queuedBuffers.TryDequeue(out _);

                    needUpdate = true;
                }

                Interlocked.Add(ref _playedSampleCount, playedAudioBufferSampleCount);
            }

            // Notify the output if needed.
            if (needUpdate)
            {
                _updateRequiredEvent.Set();
            }
        }

        public override ulong GetPlayedSampleCount()
        {
            return Interlocked.Read(ref _playedSampleCount);
        }

        public override float GetVolume()
        {
            // TODO
            return 1.0f;
        }

        public override void PrepareToClose() { }

        public override void QueueBuffer(AudioBuffer buffer)
        {
            SDL2AudioBuffer driverBuffer = new SDL2AudioBuffer(buffer.DataPointer, GetSampleCount(buffer));

            _ringBuffer.Write(buffer.Data, 0, buffer.Data.Length);

            _queuedBuffers.Enqueue(driverBuffer);
        }

        public override void SetVolume(float volume)
        {
            // TODO
        }

        public override void Start()
        {
            SDL_PauseAudioDevice(_outputStream, 0);
        }

        public override void Stop()
        {
            SDL_PauseAudioDevice(_outputStream, 1);
        }

        public override void UnregisterBuffer(AudioBuffer buffer) { }

        public override bool WasBufferFullyConsumed(AudioBuffer buffer)
        {
            if (!_queuedBuffers.TryPeek(out SDL2AudioBuffer driverBuffer))
            {
                return true;
            }

            return driverBuffer.DriverIdentifier != buffer.DataPointer;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                PrepareToClose();
                Stop();

                SDL_CloseAudioDevice(_outputStream);

                _driver.Unregister(this);
            }
        }

        public override void Dispose()
        {
            Dispose(true);
        }
    }
}
