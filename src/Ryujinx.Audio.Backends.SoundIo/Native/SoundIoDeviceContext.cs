using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ryujinx.Audio.Backends.SoundIo.Native.SoundIo;

namespace Ryujinx.Audio.Backends.SoundIo.Native
{
    public class SoundIoDeviceContext
    {
        public IntPtr Context { get; }

        internal SoundIoDeviceContext(IntPtr context)
        {
            Context = context;
        }

        private ref SoundIoDevice GetDeviceContext()
        {
            unsafe
            {
                return ref Unsafe.AsRef<SoundIoDevice>((SoundIoDevice*)Context);
            }
        }

        public bool IsRaw => GetDeviceContext().IsRaw;

        public string Id => Marshal.PtrToStringAnsi(GetDeviceContext().Id);

        public bool SupportsSampleRate(int sampleRate) => soundio_device_supports_sample_rate(Context, sampleRate);

        public bool SupportsFormat(SoundIoFormat format) => soundio_device_supports_format(Context, format);

        public bool SupportsChannelCount(int channelCount) => soundio_device_supports_layout(Context, SoundIoChannelLayout.GetDefault(channelCount));

        public SoundIoOutStreamContext CreateOutStream()
        {
            IntPtr context = soundio_outstream_create(Context);

            if (context == IntPtr.Zero)
            {
                return null;
            }

            return new SoundIoOutStreamContext(context);
        }
    }
}