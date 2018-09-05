using SoundIOSharp;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ryujinx.Audio.SoundIo
{
    internal class SoundIoAudioTrack : IDisposable
    {
        /// <summary>
        /// The audio track ring buffer
        /// </summary>
        private SoundIoRingBuffer m_Buffer;

        /// <summary>
        /// A list of buffers currently pending writeback to the audio backend
        /// </summary>
        private ConcurrentQueue<SoundIoBuffer> m_ReservedBuffers;

        /// <summary>
        /// Occurs when a buffer has been released by the audio backend
        /// </summary>
        private event ReleaseCallback BufferReleased;

        /// <summary>
        /// The track ID of this <see cref="SoundIoAudioTrack"/>
        /// </summary>
        public int TrackID { get; private set; }

        /// <summary>
        /// The current playback state
        /// </summary>
        public PlaybackState State { get; private set; }

        /// <summary>
        /// The <see cref="SoundIO"/> audio context this track belongs to
        /// </summary>
        public SoundIO AudioContext { get; private set; }

        /// <summary>
        /// The <see cref="SoundIODevice"/> this track belongs to
        /// </summary>
        public SoundIODevice AudioDevice { get; private set; }

        /// <summary>
        /// The audio output stream of this track
        /// </summary>
        public SoundIOOutStream AudioStream { get; private set; }

        /// <summary>
        /// Released buffers the track is no longer holding
        /// </summary>
        public ConcurrentQueue<long> ReleasedBuffers { get; private set; }

        /// <summary>
        /// Constructs a new instance of a <see cref="SoundIoAudioTrack"/>
        /// </summary>
        /// <param name="trackId"></param>
        /// <param name="audioContext"></param>
        /// <param name="audioDevice"></param>
        public SoundIoAudioTrack(int trackId, SoundIO audioContext, SoundIODevice audioDevice)
        {
            TrackID = trackId;
            AudioContext = audioContext;
            AudioDevice = audioDevice;
            State = PlaybackState.Stopped;
            ReleasedBuffers = new ConcurrentQueue<long>();

            m_Buffer = new SoundIoRingBuffer();
            m_ReservedBuffers = new ConcurrentQueue<SoundIoBuffer>();
        }

        /// <summary>
        /// Opens the audio track with the specified parameters
        /// </summary>
        /// <param name="sampleRate">The requested sample rate of the track</param>
        /// <param name="channelCount">The requested channel count of the track</param>
        /// <param name="callback">A <see cref="ReleaseCallback" /> that represents the delegate to invoke when a buffer has been released by the audio track</param>
        /// <param name="format">The requested sample format of the track</param>
        public void Open(int sampleRate, int channelCount, ReleaseCallback callback, SoundIOFormat format = SoundIOFormat.S16LE)
        {
            // Close any existing audio streams
            if (AudioStream != null)
                Close();

            if (!AudioDevice.SupportsSampleRate(sampleRate))
                throw new InvalidOperationException($"This sound device does not support a sample rate of {sampleRate}Hz");

            if (!AudioDevice.SupportsFormat(format))
                throw new InvalidOperationException($"This sound device does not support SoundIOFormat.{Enum.GetName(typeof(SoundIOFormat), format)}");

            AudioStream = AudioDevice.CreateOutStream();

            AudioStream.Name = $"SwitchAudioTrack_{TrackID}";
            AudioStream.SampleRate = sampleRate;
            AudioStream.Layout = SoundIOChannelLayout.GetDefault(channelCount);
            AudioStream.Format = format;

            AudioStream.WriteCallback = WriteCallback;
            //AudioStream.ErrorCallback = ErrorCallback;
            //AudioStream.UnderflowCallback = UnderflowCallback;

            BufferReleased += callback;

            AudioStream.Open();
        }

        /// <summary>
        /// This callback occurs when the sound device is ready to buffer more frames
        /// </summary>
        /// <param name="minFrameCount">The minimum amount of frames expected by the audio backend</param>
        /// <param name="maxFrameCount">The maximum amount of frames that can be written to the audio backend</param>
        private unsafe void WriteCallback(int minFrameCount, int maxFrameCount)
        {
            var bytesPerFrame = AudioStream.BytesPerFrame;
            var bytesPerSample = (uint)AudioStream.BytesPerSample;

            var bufferedFrames = m_Buffer.Length / bytesPerFrame;
            var bufferedSamples = m_Buffer.Length / bytesPerSample;

            var frameCount = Math.Min(bufferedFrames, maxFrameCount);

            if (frameCount == 0)
                return;

            var areas = AudioStream.BeginWrite(ref frameCount);
            var channelCount = areas.ChannelCount;

            var samples = new byte[frameCount * bytesPerFrame];

            m_Buffer.Read(samples, 0, samples.Length);

            // This is a huge ugly block of code, but we save
            // a significant amount of time over the generic
            // loop that handles other channel counts.
            
            // Mono
            if (channelCount == 1)
            {
                var area = areas.GetArea(0);

                fixed (byte* buffPtr = &samples[0])
                {
                    if (bytesPerSample == 1)
                    {
                        for (var frame = 0; frame < frameCount; frame++)
                        {
                            *((byte*)area.Pointer) = *(buffPtr + (frame * bytesPerFrame));

                            area.Pointer += area.Step;
                        }
                    }
                    else if (bytesPerSample == 2)
                    {
                        for (var frame = 0; frame < frameCount; frame++)
                        {
                            *((byte*)area.Pointer + 0) = *(buffPtr + (frame * bytesPerFrame) + 0);
                            *((byte*)area.Pointer + 1) = *(buffPtr + (frame * bytesPerFrame) + 1);

                            area.Pointer += area.Step;
                        }
                    }
                    else if (bytesPerSample == 4)
                    {
                        for (var frame = 0; frame < frameCount; frame++)
                        {
                            *((byte*)area.Pointer + 0) = *(buffPtr + (frame * bytesPerFrame) + 0);
                            *((byte*)area.Pointer + 1) = *(buffPtr + (frame * bytesPerFrame) + 1);
                            *((byte*)area.Pointer + 2) = *(buffPtr + (frame * bytesPerFrame) + 2);
                            *((byte*)area.Pointer + 3) = *(buffPtr + (frame * bytesPerFrame) + 3);

                            area.Pointer += area.Step;
                        }
                    }
                    else
                    {
                        for (var frame = 0; frame < frameCount; frame++)
                        {
                            Unsafe.CopyBlockUnaligned((byte*)area.Pointer, buffPtr + (frame * bytesPerFrame), bytesPerSample);

                            area.Pointer += area.Step;
                        }
                    }
                }
            }
            // Stereo
            else if (channelCount == 2)
            {
                var area1 = areas.GetArea(0);
                var area2 = areas.GetArea(1);

                fixed (byte* buffPtr = &samples[0])
                {
                    if (bytesPerSample == 1)
                    {
                        for (var frame = 0; frame < frameCount; frame++)
                        {
                            // Channel 1
                            *((byte*)area1.Pointer) = *(buffPtr + (frame * bytesPerFrame) + (0 * bytesPerSample));

                            // Channel 2
                            *((byte*)area2.Pointer) = *(buffPtr + (frame * bytesPerFrame) + (1 * bytesPerSample));

                            area1.Pointer += area1.Step;
                            area2.Pointer += area2.Step;
                        }
                    }
                    else if (bytesPerSample == 2)
                    {
                        for (var frame = 0; frame < frameCount; frame++)
                        {
                            // Channel 1
                            *((byte*)area1.Pointer + 0) = *(buffPtr + (frame * bytesPerFrame) + (0 * bytesPerSample) + 0);
                            *((byte*)area1.Pointer + 1) = *(buffPtr + (frame * bytesPerFrame) + (0 * bytesPerSample) + 1);

                            // Channel 2
                            *((byte*)area2.Pointer + 0) = *(buffPtr + (frame * bytesPerFrame) + (1 * bytesPerSample) + 0);
                            *((byte*)area2.Pointer + 1) = *(buffPtr + (frame * bytesPerFrame) + (1 * bytesPerSample) + 1);

                            area1.Pointer += area1.Step;
                            area2.Pointer += area2.Step;
                        }
                    }
                    else if (bytesPerSample == 4)
                    {
                        for (var frame = 0; frame < frameCount; frame++)
                        {
                            // Channel 1
                            *((byte*)area1.Pointer + 0) = *(buffPtr + (frame * bytesPerFrame) + (0 * bytesPerSample) + 0);
                            *((byte*)area1.Pointer + 1) = *(buffPtr + (frame * bytesPerFrame) + (0 * bytesPerSample) + 1);
                            *((byte*)area1.Pointer + 2) = *(buffPtr + (frame * bytesPerFrame) + (0 * bytesPerSample) + 2);
                            *((byte*)area1.Pointer + 3) = *(buffPtr + (frame * bytesPerFrame) + (0 * bytesPerSample) + 3);

                            // Channel 2
                            *((byte*)area2.Pointer + 0) = *(buffPtr + (frame * bytesPerFrame) + (1 * bytesPerSample) + 0);
                            *((byte*)area2.Pointer + 1) = *(buffPtr + (frame * bytesPerFrame) + (1 * bytesPerSample) + 1);
                            *((byte*)area2.Pointer + 2) = *(buffPtr + (frame * bytesPerFrame) + (1 * bytesPerSample) + 2);
                            *((byte*)area2.Pointer + 3) = *(buffPtr + (frame * bytesPerFrame) + (1 * bytesPerSample) + 3);

                            area1.Pointer += area1.Step;
                            area2.Pointer += area2.Step;
                        }
                    }
                    else
                    {
                        for (var frame = 0; frame < frameCount; frame++)
                        {
                            Unsafe.CopyBlockUnaligned((byte*)area1.Pointer, buffPtr + (frame * bytesPerFrame) + (0 * bytesPerSample), bytesPerSample);
                            Unsafe.CopyBlockUnaligned((byte*)area2.Pointer, buffPtr + (frame * bytesPerFrame) + (1 * bytesPerSample), bytesPerSample);

                            area1.Pointer += area1.Step;
                            area2.Pointer += area2.Step;
                        }
                    }
                }
            }
            // Surround
            else if (channelCount == 5)
            {
                var area1 = areas.GetArea(0);
                var area2 = areas.GetArea(1);
                var area3 = areas.GetArea(2);
                var area4 = areas.GetArea(3);
                var area5 = areas.GetArea(4);

                fixed (byte* buffPtr = &samples[0])
                {
                    if (bytesPerSample == 1)
                    {
                        for (var frame = 0; frame < frameCount; frame++)
                        {
                            // Channel 1
                            *((byte*)area1.Pointer) = *(buffPtr + (frame * bytesPerFrame) + (0 * bytesPerSample));

                            // Channel 2
                            *((byte*)area2.Pointer) = *(buffPtr + (frame * bytesPerFrame) + (1 * bytesPerSample));

                            // Channel 3
                            *((byte*)area3.Pointer) = *(buffPtr + (frame * bytesPerFrame) + (2 * bytesPerSample));

                            // Channel 4
                            *((byte*)area4.Pointer) = *(buffPtr + (frame * bytesPerFrame) + (3 * bytesPerSample));

                            // Channel 5
                            *((byte*)area5.Pointer) = *(buffPtr + (frame * bytesPerFrame) + (4 * bytesPerSample));

                            area1.Pointer += area1.Step;
                            area2.Pointer += area2.Step;
                            area3.Pointer += area3.Step;
                            area4.Pointer += area4.Step;
                            area5.Pointer += area5.Step;
                        }
                    }
                    else if (bytesPerSample == 2)
                    {
                        for (var frame = 0; frame < frameCount; frame++)
                        {
                            // Channel 1
                            *((byte*)area1.Pointer + 0) = *(buffPtr + (frame * bytesPerFrame) + (0 * bytesPerSample) + 0);
                            *((byte*)area1.Pointer + 1) = *(buffPtr + (frame * bytesPerFrame) + (0 * bytesPerSample) + 1);

                            // Channel 2
                            *((byte*)area2.Pointer + 0) = *(buffPtr + (frame * bytesPerFrame) + (1 * bytesPerSample) + 0);
                            *((byte*)area2.Pointer + 1) = *(buffPtr + (frame * bytesPerFrame) + (1 * bytesPerSample) + 1);

                            // Channel 3
                            *((byte*)area3.Pointer + 0) = *(buffPtr + (frame * bytesPerFrame) + (2 * bytesPerSample) + 0);
                            *((byte*)area3.Pointer + 1) = *(buffPtr + (frame * bytesPerFrame) + (2 * bytesPerSample) + 1);

                            // Channel 4
                            *((byte*)area4.Pointer + 0) = *(buffPtr + (frame * bytesPerFrame) + (3 * bytesPerSample) + 0);
                            *((byte*)area4.Pointer + 1) = *(buffPtr + (frame * bytesPerFrame) + (3 * bytesPerSample) + 1);

                            // Channel 5
                            *((byte*)area5.Pointer + 0) = *(buffPtr + (frame * bytesPerFrame) + (4 * bytesPerSample) + 0);
                            *((byte*)area5.Pointer + 1) = *(buffPtr + (frame * bytesPerFrame) + (4 * bytesPerSample) + 1);

                            area1.Pointer += area1.Step;
                            area2.Pointer += area2.Step;
                            area3.Pointer += area3.Step;
                            area4.Pointer += area4.Step;
                            area5.Pointer += area5.Step;
                        }
                    }
                    else if (bytesPerSample == 4)
                    {
                        for (var frame = 0; frame < frameCount; frame++)
                        {
                            // Channel 1
                            *((byte*)area1.Pointer + 0) = *(buffPtr + (frame * bytesPerFrame) + (0 * bytesPerSample) + 0);
                            *((byte*)area1.Pointer + 1) = *(buffPtr + (frame * bytesPerFrame) + (0 * bytesPerSample) + 1);
                            *((byte*)area1.Pointer + 2) = *(buffPtr + (frame * bytesPerFrame) + (0 * bytesPerSample) + 2);
                            *((byte*)area1.Pointer + 3) = *(buffPtr + (frame * bytesPerFrame) + (0 * bytesPerSample) + 3);

                            // Channel 2
                            *((byte*)area2.Pointer + 0) = *(buffPtr + (frame * bytesPerFrame) + (1 * bytesPerSample) + 0);
                            *((byte*)area2.Pointer + 1) = *(buffPtr + (frame * bytesPerFrame) + (1 * bytesPerSample) + 1);
                            *((byte*)area2.Pointer + 2) = *(buffPtr + (frame * bytesPerFrame) + (1 * bytesPerSample) + 2);
                            *((byte*)area2.Pointer + 3) = *(buffPtr + (frame * bytesPerFrame) + (1 * bytesPerSample) + 3);

                            // Channel 3
                            *((byte*)area3.Pointer + 0) = *(buffPtr + (frame * bytesPerFrame) + (2 * bytesPerSample) + 0);
                            *((byte*)area3.Pointer + 1) = *(buffPtr + (frame * bytesPerFrame) + (2 * bytesPerSample) + 1);
                            *((byte*)area3.Pointer + 2) = *(buffPtr + (frame * bytesPerFrame) + (2 * bytesPerSample) + 2);
                            *((byte*)area3.Pointer + 3) = *(buffPtr + (frame * bytesPerFrame) + (2 * bytesPerSample) + 3);

                            // Channel 4
                            *((byte*)area4.Pointer + 0) = *(buffPtr + (frame * bytesPerFrame) + (3 * bytesPerSample) + 0);
                            *((byte*)area4.Pointer + 1) = *(buffPtr + (frame * bytesPerFrame) + (3 * bytesPerSample) + 1);
                            *((byte*)area4.Pointer + 2) = *(buffPtr + (frame * bytesPerFrame) + (3 * bytesPerSample) + 2);
                            *((byte*)area4.Pointer + 3) = *(buffPtr + (frame * bytesPerFrame) + (3 * bytesPerSample) + 3);

                            // Channel 5
                            *((byte*)area5.Pointer + 0) = *(buffPtr + (frame * bytesPerFrame) + (4 * bytesPerSample) + 0);
                            *((byte*)area5.Pointer + 1) = *(buffPtr + (frame * bytesPerFrame) + (4 * bytesPerSample) + 1);
                            *((byte*)area5.Pointer + 2) = *(buffPtr + (frame * bytesPerFrame) + (4 * bytesPerSample) + 2);
                            *((byte*)area5.Pointer + 3) = *(buffPtr + (frame * bytesPerFrame) + (4 * bytesPerSample) + 3);

                            area1.Pointer += area1.Step;
                            area2.Pointer += area2.Step;
                            area3.Pointer += area3.Step;
                            area4.Pointer += area4.Step;
                            area5.Pointer += area5.Step;
                        }
                    }
                    else
                    {
                        for (var frame = 0; frame < frameCount; frame++)
                        {
                            Unsafe.CopyBlockUnaligned((byte*)area1.Pointer, buffPtr + (frame * bytesPerFrame) + (0 * bytesPerSample), bytesPerSample);
                            Unsafe.CopyBlockUnaligned((byte*)area2.Pointer, buffPtr + (frame * bytesPerFrame) + (1 * bytesPerSample), bytesPerSample);

                            area1.Pointer += area1.Step;
                            area2.Pointer += area2.Step;
                        }
                    }
                }
            }
            // Every other channel count
            else
            {
                var channels = new SoundIOChannelArea[channelCount];

                // Obtain the channel area for each channel
                for (var i = 0; i < channelCount; i++)
                    channels[i] = areas.GetArea(i);

                fixed (byte* buffPtr = &samples[0])
                {
                    for (var frame = 0; frame < frameCount; frame++)
                    for (var channel = 0; channel < areas.ChannelCount; channel++)
                    {
                        // This is slow!
                        Unsafe.CopyBlockUnaligned((byte*)channels[channel].Pointer, buffPtr + frame * bytesPerFrame + channel * bytesPerSample, bytesPerSample);
                        channels[channel].Pointer += channels[channel].Step;
                    }
                }
            }

            AudioStream.EndWrite();

            UpdateReleasedBuffers(samples.Length);
        }

        /// <summary>
        /// Releases any buffers that have been fully written to the output device
        /// </summary>
        /// <param name="bytesRead">The amount of bytes written in the last device write</param>
        private void UpdateReleasedBuffers(int bytesRead)
        {
            bool bufferReleased = false;
            while (bytesRead > 0)
            {
                if (m_ReservedBuffers.TryPeek(out SoundIoBuffer buffer))
                {
                    if (buffer.Length > bytesRead)
                    {
                        buffer.Length -= bytesRead;
                        bytesRead = 0;
                    }
                    else
                    {
                        bufferReleased = true;
                        bytesRead -= buffer.Length;

                        m_ReservedBuffers.TryDequeue(out buffer);
                        ReleasedBuffers.Enqueue(buffer.Tag);
                    }
                }
            }

            if (bufferReleased)
            {
                OnBufferReleased();
            }
        }

        /// <summary>
        /// This callback occurs when the sound device encounters an error
        /// </summary>
        private void ErrorCallback()
        {

        }

        /// <summary>
        /// This callback occurs when the sound device runs out of buffered audio data to play
        /// </summary>
        private void UnderflowCallback()
        {

        }

        /// <summary>
        /// Starts audio playback
        /// </summary>
        public void Start()
        {
            if (AudioStream == null)
                return;

            AudioStream.Start();
            AudioStream.Pause(false);
            AudioContext.FlushEvents();
            State = PlaybackState.Playing;
        }

        /// <summary>
        /// Stops audio playback
        /// </summary>
        public void Stop()
        {
            if (AudioStream == null)
                return;

            AudioStream.Pause(true);
            AudioContext.FlushEvents();
            State = PlaybackState.Stopped;
        }

        /// <summary>
        /// Appends an audio buffer to the tracks internal ring buffer
        /// </summary>
        /// <typeparam name="T">The audio sample type</typeparam>
        /// <param name="bufferTag">The unqiue tag of the buffer being appended</param>
        /// <param name="buffer">The buffer to append</param>
        public void AppendBuffer<T>(long bufferTag, T[] buffer)
        {
            if (AudioStream == null)
                return;

            // Calculate the size of the audio samples
            var size = TypeSize<T>.Size;

            // Calculate the amount of bytes to copy from the buffer
            var bytesToCopy = size * buffer.Length;

            // Copy the memory to our ring buffer
            m_Buffer.Write(buffer, 0, bytesToCopy);

            // Keep track of "buffered" buffers
            m_ReservedBuffers.Enqueue(new SoundIoBuffer(bufferTag, bytesToCopy));
        }

        /// <summary>
        /// Returns a value indicating whether the specified buffer is currently reserved by the track
        /// </summary>
        /// <param name="bufferTag">The buffer tag to check</param>
        public bool ContainsBuffer(long bufferTag)
        {
            return m_ReservedBuffers.Any(x => x.Tag == bufferTag);
        }

        /// <summary>
        /// Closes the <see cref="SoundIoAudioTrack"/>
        /// </summary>
        public void Close()
        {
            if (AudioStream != null)
            {
                AudioStream.Pause(true);
                AudioStream.Dispose();
            }

            m_Buffer.Clear();
            OnBufferReleased();
            ReleasedBuffers.Clear();

            AudioStream = null;
            BufferReleased = null;
            State = PlaybackState.Stopped;
        }

        private void OnBufferReleased()
        {
            BufferReleased?.Invoke();
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="SoundIoAudioTrack" />
        /// </summary>
        public void Dispose()
        {
            Close();
        }

        ~SoundIoAudioTrack()
        {
            Dispose();
        }
    }
}
