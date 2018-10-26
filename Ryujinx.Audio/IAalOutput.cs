using System;

namespace Ryujinx.Audio
{
    public interface IAalOutput : IDisposable
    {
        int OpenTrack(int sampleRate, int channels, ReleaseCallback callback);

        void CloseTrack(int track);

        bool ContainsBuffer(int track, long tag);

        long[] GetReleasedBuffers(int track, int maxCount);

        void AppendBuffer<T>(int track, long tag, T[] buffer)  where T : struct;

        void Start(int track);
        void Stop(int track);

        PlaybackState GetState(int track);
    }
}