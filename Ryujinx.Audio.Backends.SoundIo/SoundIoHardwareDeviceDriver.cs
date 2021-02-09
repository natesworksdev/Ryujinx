using Ryujinx.Audio.Common;
using Ryujinx.Audio.Integration;
using Ryujinx.Memory;
using SoundIOSharp;
using System;
using System.Collections.Generic;
using System.Threading;

using static Ryujinx.Audio.Integration.HardwareDeviceDriver;

namespace Ryujinx.Audio.Backends.SoundIo
{
    public class SoundIoHardwareDeviceDriver : HardwareDeviceDriver
    {
        private object _lock = new object();

        private SoundIO _audioContext;
        private SoundIODevice _audioDevice;
        private ManualResetEvent _updateRequiredEvent;
        private List<SoundIoHardwareDeviceSession> _sessions;

        public SoundIoHardwareDeviceDriver()
        {
            _audioContext = new SoundIO();
            _updateRequiredEvent = new ManualResetEvent(false);
            _sessions = new List<SoundIoHardwareDeviceSession>();

            _audioContext.Connect();
            _audioContext.FlushEvents();

            _audioDevice = FindNonRawDefaultAudioDevice(_audioContext, true);
        }

        public static bool IsSupported => IsSupportedInternal();

        private static bool IsSupportedInternal()
        {
            SoundIO context = null;
            SoundIODevice device = null;
            SoundIOOutStream stream = null;

            bool backendDisconnected = false;

            try
            {
                context = new SoundIO();

                context.OnBackendDisconnect = (i) => {
                    backendDisconnected = true;
                };

                context.Connect();
                context.FlushEvents();

                if (backendDisconnected)
                {
                    return false;
                }

                if (context.OutputDeviceCount == 0)
                {
                    return false;
                }

                device = FindNonRawDefaultAudioDevice(context);

                if (device == null || backendDisconnected)
                {
                    return false;
                }

                stream = device.CreateOutStream();

                if (stream == null || backendDisconnected)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }

                if (context != null)
                {
                    context.Dispose();
                }
            }
        }

        private static SoundIODevice FindNonRawDefaultAudioDevice(SoundIO audioContext, bool fallback = false)
        {
            SoundIODevice defaultAudioDevice = audioContext.GetOutputDevice(audioContext.DefaultOutputDeviceIndex);

            if (!defaultAudioDevice.IsRaw)
            {
                return defaultAudioDevice;
            }

            for (int i = 0; i < audioContext.BackendCount; i++)
            {
                SoundIODevice audioDevice = audioContext.GetOutputDevice(i);

                if (audioDevice.Id == defaultAudioDevice.Id && !audioDevice.IsRaw)
                {
                    return audioDevice;
                }
            }

            return fallback ? defaultAudioDevice : null;
        }

        public ManualResetEvent GetUpdateRequiredEvent()
        {
            return _updateRequiredEvent;
        }

        public HardwareDeviceSession OpenDeviceSession(Direction direction, IVirtualMemoryManager memoryManager, SampleFormat sampleFormat, uint sampleRate, uint channelCount)
        {
            if (direction != Direction.Output)
            {
                // TODO
                throw new NotImplementedException();
            }

            if (channelCount == 0)
            {
                channelCount = 2;
            }

            lock (_lock)
            {
                SoundIoHardwareDeviceSession session = new SoundIoHardwareDeviceSession(this, memoryManager, sampleFormat, sampleRate, channelCount);

                _sessions.Add(session);

                return session;
            }
        }

        internal void Unregister(SoundIoHardwareDeviceSession session)
        {
            lock (_lock)
            {
                _sessions.Remove(session);
            }
        }

        public static SoundIOFormat GetSoundIoFormat(SampleFormat format)
        {
            switch (format)
            {
                case SampleFormat.PcmInt8:
                    return SoundIOFormat.S8;
                case SampleFormat.PcmInt16:
                    return SoundIOFormat.S16LE;
                case SampleFormat.PcmInt24:
                    return SoundIOFormat.S24LE;
                case SampleFormat.PcmInt32:
                    return SoundIOFormat.S32LE;
                case SampleFormat.PcmFloat:
                    return SoundIOFormat.Float32LE;
                default:
                    throw new NotImplementedException($"Unsupported sample format {format}");
            }
        }

        internal SoundIOOutStream OpenStream(SampleFormat requestedSampleFormat, uint requestedSampleRate, uint requestedChannelCount)
        {
            SoundIOFormat driverSampleFormat = GetSoundIoFormat(requestedSampleFormat);

            if (!_audioDevice.SupportsSampleRate((int)requestedSampleRate))
            {
                throw new InvalidOperationException($"This sound device does not support a sample rate of {requestedSampleRate}Hz");
            }

            if (!_audioDevice.SupportsFormat(driverSampleFormat))
            {
                throw new InvalidOperationException($"This sound device does not support SampleFormat.{requestedSampleFormat}");
            }

            if (!_audioDevice.SupportsChannelCount((int)requestedChannelCount))
            {
                throw new InvalidOperationException($"This sound device does not support channel count {requestedChannelCount}");
            }

            SoundIOOutStream result = _audioDevice.CreateOutStream();

            result.Name = $"Ryujinx";
            result.Layout = SoundIOChannelLayout.GetDefault((int)requestedChannelCount);
            result.Format = driverSampleFormat;
            result.SampleRate = (int)requestedSampleRate;

            return result;
        }

        internal void FlushContextEvents()
        {
            _audioContext.FlushEvents();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                while (_sessions.Count > 0)
                {
                    SoundIoHardwareDeviceSession session = _sessions[_sessions.Count - 1];

                    session.Dispose();
                }

                _audioContext.Disconnect();
                _audioContext.Dispose();
            }
        }

        public bool SupportsSampleRate(uint sampleRate)
        {
            return _audioDevice.SupportsSampleRate((int)sampleRate);
        }

        public bool SupportsSampleFormat(SampleFormat sampleFormat)
        {
            return _audioDevice.SupportsFormat(GetSoundIoFormat(sampleFormat));
        }

        public bool SupportsChannelCount(uint channelCount)
        {
            return _audioDevice.SupportsChannelCount((int)channelCount);
        }

        public bool SupportsDirection(Direction direction)
        {
            // TODO: add direction input when supported.
            return direction == Direction.Output;
        }
    }
}
