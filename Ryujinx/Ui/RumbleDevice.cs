using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Hid;
using SDL2;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Ryujinx.Ui
{
    class RumbleDevice
    {
        private bool _dllLoaded = true;
        private Thread _thread;
        private bool _rumbleSupported;
        private IntPtr _haptic;
        private ConcurrentQueue<HidVibrationValue> _vibrationQueue;
        private SDL.SDL_HapticEffect _effect = new SDL.SDL_HapticEffect
        {
            type = SDL.SDL_HAPTIC_LEFTRIGHT,
            leftright = new SDL.SDL_HapticLeftRight
            {
                type = SDL.SDL_HAPTIC_LEFTRIGHT,
                length = 1,
                small_magnitude = 0,
                large_magnitude = 0,
            },
        };
        public HidVibrationValue LastVibrationValue { get; private set; } = new HidVibrationValue();

        public RumbleDevice(int index, ConcurrentQueue<HidVibrationValue> vibrationQueue)
        {
            try
            {
                if (SDL.SDL_Init(SDL.SDL_INIT_HAPTIC) != 0)
                {
                    Logger.Error?.PrintMsg(LogClass.ServiceHid, "Failed to initialize SDL, error = " + SDL.SDL_GetError());
                }
                _haptic = SDL.SDL_HapticOpen(index);
                if (_haptic == IntPtr.Zero)
                {
                    Logger.Info?.PrintMsg(LogClass.ServiceHid, "Haptic device is null!");
                    Logger.Info?.PrintMsg(LogClass.ServiceHid, "SDL error = " + SDL.SDL_GetError());
                }
                if (_haptic == IntPtr.Zero || SDL.SDL_HapticEffectSupported(_haptic, ref _effect) == 0)
                {
                    if (SDL.SDL_HapticEffectSupported(_haptic, ref _effect) == 0) Logger.Info?.PrintMsg(LogClass.ServiceHid, "Haptic effect leftright not supported!");
                    _rumbleSupported = false;
                }
                else
                {
                    _rumbleSupported = true;
                }
                Logger.Info?.PrintMsg(LogClass.ServiceHid, "Initialized rumble device, actual support = " + _rumbleSupported);
            }
            catch (DllNotFoundException)
            {
                Logger.Info?.PrintMsg(LogClass.ServiceHid, "SDL2 DLL not found, silently stubbing");
                _dllLoaded = false;
            }
            _vibrationQueue = vibrationQueue;
        }

        public void ThreadProc()
        {
            while (_vibrationQueue.Count <= 0)
            {
                // Yield until we start getting values
                Thread.Yield();
            }
            while (true)
            {
                while (_vibrationQueue.TryDequeue(out HidVibrationValue value))
                {
                    _effect.leftright.small_magnitude = (ushort)ProcessAmplitude(value.AmplitudeLow);
                    _effect.leftright.large_magnitude = (ushort)ProcessAmplitude(value.AmplitudeHigh);
                    int effectIndex = SDL.SDL_HapticNewEffect(_haptic, ref _effect);
                    SDL.SDL_HapticRunEffect(_haptic, effectIndex, SDL.SDL_HAPTIC_INFINITY);
                    SDL.SDL_HapticDestroyEffect(_haptic, effectIndex);
                    LastVibrationValue = value;
                    //Keeping this here for future debugging.
                    //Logger.Warning?.PrintMsg(LogClass.Hid, $"Vibrating, AmpLow:{value.AmplitudeLow} | AmpHigh:{value.AmplitudeHigh} | FrecLow:{value.FrequencyLow} | FrecHigh:{value.FrequencyHigh}");
                    //Logger.Warning?.PrintMsg(LogClass.Hid, $"Queue : {_vibrationQueue.Count}");
                }
            }
        }

        public bool RumbleDataEqual(HidVibrationValue value1, HidVibrationValue value2)
        {
            return value1.AmplitudeHigh == value2.AmplitudeHigh
                && value1.AmplitudeLow == value2.AmplitudeLow;
        }

        private static ushort ProcessAmplitude(float x)
        {
            // Credit to @Morph1984 for this formula
            return (ushort)(Math.Pow(x, 0.5f) * (3.0f - 2.0f * Math.Pow(x, 015f)) * 0xFFFF);
        }

        public void Start()
        {
            // Check if dll is not loaded or rumble is not supported
            if (!_dllLoaded || !_rumbleSupported) return;
            _thread = new Thread(() => ThreadProc());
            _thread.Start();
        }

        ~RumbleDevice()
        {
            if (_thread != null) _thread.Join(500);
            if (_dllLoaded)
            {
                if (_haptic != IntPtr.Zero)
                {
                    SDL.SDL_HapticClose(_haptic);
                }
            }
        }
    }
}
