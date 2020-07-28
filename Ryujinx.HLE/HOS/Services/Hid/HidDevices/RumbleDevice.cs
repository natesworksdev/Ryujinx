using OpenTK.Input;
using Ryujinx.Common.Logging;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Ryujinx.HLE.HOS.Services.Hid
{
    public class RumbleDevice
    {
        private int _gamepadIndex;
        public HidVibrationValue LastValue = new HidVibrationValue();

        public RumbleDevice(int gamepadIndex)
        {
            _gamepadIndex = gamepadIndex;
        }

        public void RumbleMultiple(ReadOnlySpan<HidVibrationValue> values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                HidVibrationValue value = values[i];
                float amplitude = Math.Max(value.AmplitudeLow, value.AmplitudeHigh);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Unsafe access to internal XInput joystick for proper vibration
                    Type XInputJoystick = Type.GetType("OpenTK.Platform.Windows.XInputJoystick, OpenTK, Version=1.0.5.12, Culture=neutral, PublicKeyToken=bad199fe84eb3df4");
                    Trace.Assert(XInputJoystick != null);
                    object joystick = XInputJoystick.GetConstructor(Type.EmptyTypes).Invoke(null);
                    Trace.Assert(joystick != null);
                    XInputJoystick.GetMethod("SetVibration").Invoke(joystick, new object[] { _gamepadIndex, amplitude, amplitude });
                } else
                {
                    // No-op
                    GamePad.SetVibration(_gamepadIndex, amplitude, amplitude);
                }
                if (i == values.Length - 1)
                {
                    LastValue = value;
                }
            }
        }

        public void Rumble(HidVibrationValue value)
        {
            RumbleMultiple(new HidVibrationValue[] { value });
        }
    }
}
