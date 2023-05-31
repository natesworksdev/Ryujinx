using System;
using System.Runtime.InteropServices;
using static Ryujinx.Audio.Backends.SoundIo.Native.SoundIo;

namespace Ryujinx.Audio.Backends.SoundIo.Native
{
    internal sealed class SoundIoException : Exception
    {
        internal SoundIoException(SoundIoError error) : base(Marshal.PtrToStringAnsi(soundio_strerror(error))) { }
    }
}
