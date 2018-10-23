using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ryujinx.Audio.OpenAL
{
    public class OpenAlAudioOut : IAalOutput, IDisposable
    {
        private const int MaxTracks = 256;

        private const int MaxReleased = 32;

        private AudioContext _context;

        private class Track : IDisposable
        {
            public int SourceId { get; private set; }

            public int SampleRate { get; private set; }

            public ALFormat Format { get; private set; }

            private ReleaseCallback _callback;

            public PlaybackState State { get; set; }

            private ConcurrentDictionary<long, int> _buffers;

            private Queue<long> _queuedTagsQueue;

            private Queue<long> _releasedTagsQueue;

            private bool _disposed;

            public Track(int sampleRate, ALFormat format, ReleaseCallback callback)
            {
                SampleRate = sampleRate;
                Format     = format;
                _callback   = callback;

                State = PlaybackState.Stopped;

                SourceId = AL.GenSource();

                _buffers = new ConcurrentDictionary<long, int>();

                _queuedTagsQueue = new Queue<long>();

                _releasedTagsQueue = new Queue<long>();
            }

            public bool ContainsBuffer(long tag)
            {
                foreach (long queuedTag in _queuedTagsQueue)
                {
                    if (queuedTag == tag)
                    {
                        return true;
                    }
                }

                return false;
            }

            public long[] GetReleasedBuffers(int count)
            {
                AL.GetSource(SourceId, ALGetSourcei.BuffersProcessed, out int releasedCount);

                releasedCount += _releasedTagsQueue.Count;

                if (count > releasedCount)
                {
                    count = releasedCount;
                }

                List<long> tags = new List<long>();

                while (count-- > 0 && _releasedTagsQueue.TryDequeue(out long tag))
                {
                    tags.Add(tag);
                }

                while (count-- > 0 && _queuedTagsQueue.TryDequeue(out long tag))
                {
                    AL.SourceUnqueueBuffers(SourceId, 1);

                    tags.Add(tag);
                }

                return tags.ToArray();
            }

            public int AppendBuffer(long tag)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(Track));
                }

                int id = AL.GenBuffer();

                _buffers.AddOrUpdate(tag, id, (key, oldId) =>
                {
                    AL.DeleteBuffer(oldId);

                    return id;
                });

                _queuedTagsQueue.Enqueue(tag);

                return id;
            }

            public void CallReleaseCallbackIfNeeded()
            {
                AL.GetSource(SourceId, ALGetSourcei.BuffersProcessed, out int releasedCount);

                if (releasedCount > 0)
                {
                    //If we signal, then we also need to have released buffers available
                    //to return when GetReleasedBuffers is called.
                    //If playback needs to be re-started due to all buffers being processed,
                    //then OpenAL zeros the counts (ReleasedCount), so we keep it on the queue.
                    while (releasedCount-- > 0 && _queuedTagsQueue.TryDequeue(out long tag))
                    {
                        AL.SourceUnqueueBuffers(SourceId, 1);

                        _releasedTagsQueue.Enqueue(tag);
                    }

                    _callback();
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing && !_disposed)
                {
                    _disposed = true;

                    AL.DeleteSource(SourceId);

                    foreach (int id in _buffers.Values)
                    {
                        AL.DeleteBuffer(id);
                    }
                }
            }
        }

        private ConcurrentDictionary<int, Track> _tracks;

        private Thread _audioPollerThread;

        private bool _keepPolling;

        public OpenAlAudioOut()
        {
            _context = new AudioContext();

            _tracks = new ConcurrentDictionary<int, Track>();

            _keepPolling = true;

            _audioPollerThread = new Thread(AudioPollerWork);

            _audioPollerThread.Start();
        }

        private void AudioPollerWork()
        {
            do
            {
                foreach (Track td in _tracks.Values)
                {
                    lock (td)
                    {
                        td.CallReleaseCallbackIfNeeded();
                    }
                }

                //If it's not slept it will waste cycles.
                Thread.Sleep(10);
            }
            while (_keepPolling);

            foreach (Track td in _tracks.Values)
            {
                td.Dispose();
            }

            _tracks.Clear();
        }

        public int OpenTrack(int sampleRate, int channels, ReleaseCallback callback)
        {
            Track td = new Track(sampleRate, GetAlFormat(channels), callback);

            for (int id = 0; id < MaxTracks; id++)
            {
                if (_tracks.TryAdd(id, td))
                {
                    return id;
                }
            }

            return -1;
        }

        private ALFormat GetAlFormat(int channels)
        {
            switch (channels)
            {
                case 1: return ALFormat.Mono16;
                case 2: return ALFormat.Stereo16;
                case 6: return ALFormat.Multi51Chn16Ext;
            }

            throw new ArgumentOutOfRangeException(nameof(channels));
        }

        public void CloseTrack(int track)
        {
            if (_tracks.TryRemove(track, out Track td))
            {
                lock (td)
                {
                    td.Dispose();
                }
            }
        }

        public bool ContainsBuffer(int track, long tag)
        {
            if (_tracks.TryGetValue(track, out Track td))
            {
                lock (td)
                {
                    return td.ContainsBuffer(tag);
                }
            }

            return false;
        }

        public long[] GetReleasedBuffers(int track, int maxCount)
        {
            if (_tracks.TryGetValue(track, out Track td))
            {
                lock (td)
                {
                    return td.GetReleasedBuffers(maxCount);
                }
            }

            return null;
        }

        public void AppendBuffer<T>(int track, long tag, T[] buffer) where T : struct
        {
            if (_tracks.TryGetValue(track, out Track td))
            {
                lock (td)
                {
                    int bufferId = td.AppendBuffer(tag);

                    int size = buffer.Length * Marshal.SizeOf<T>();

                    AL.BufferData<T>(bufferId, td.Format, buffer, size, td.SampleRate);

                    AL.SourceQueueBuffer(td.SourceId, bufferId);

                    StartPlaybackIfNeeded(td);
                }
            }
        }

        public void Start(int track)
        {
            if (_tracks.TryGetValue(track, out Track td))
            {
                lock (td)
                {
                    td.State = PlaybackState.Playing;

                    StartPlaybackIfNeeded(td);
                }
            }
        }

        private void StartPlaybackIfNeeded(Track td)
        {
            AL.GetSource(td.SourceId, ALGetSourcei.SourceState, out int stateInt);

            ALSourceState state = (ALSourceState)stateInt;

            if (state != ALSourceState.Playing && td.State == PlaybackState.Playing)
            {
                AL.SourcePlay(td.SourceId);
            }
        }

        public void Stop(int track)
        {
            if (_tracks.TryGetValue(track, out Track td))
            {
                lock (td)
                {
                    td.State = PlaybackState.Stopped;

                    AL.SourceStop(td.SourceId);
                }
            }
        }

        public PlaybackState GetState(int track)
        {
            if (_tracks.TryGetValue(track, out Track td))
            {
                return td.State;
            }

            return PlaybackState.Stopped;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _keepPolling = false;
            }
        }
    }
}