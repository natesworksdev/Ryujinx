using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Audio.Backends.SoundIo.Native
{
    public enum SoundIoError
    {
        None,
        NoMem,
        InitAudioBackend,
        SystemResources,
        OpeningDevice,
        NoSuchDevice,
        Invalid,
        BackendUnavailable,
        Streaming,
        IncompatibleDevice,
        NoSuchClient,
        IncompatibleBackend,
        BackendDisconnected,
        Interrupted,
        Underflow,
        EncodingString,
    }
}
