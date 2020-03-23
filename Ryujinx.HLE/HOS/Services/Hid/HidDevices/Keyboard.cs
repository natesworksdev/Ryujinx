using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Hid
{

    public struct KeyboardInput
    {
        public int Modifier;
        public int[] Keys;
    }

    public class KeyboardDevice : BaseDevice
    {
        public KeyboardDevice(Switch device, bool active) : base(device, active)
        {
            if (Marshal.SizeOf<HidKeyboard>() != 0x400)
            {
                throw new System.DataMisalignedException($"HidKeyboard struct is the wrong size! Expected:0x400 Got:{Marshal.SizeOf<HidKeyboard>()}");
            }
        }

        public unsafe void Update(KeyboardInput keyState)
        {
            ref HidKeyboard keyboard = ref _device.Hid.SharedMemory.Keyboard;

            int prevIndex;
            int curIndex = UpdateEntriesHeader(ref keyboard.Header, out prevIndex);

            if (!Active) return;

            ref HidKeyboardEntry curEntry = ref keyboard.Entries[curIndex];
            HidKeyboardEntry prevEntry = keyboard.Entries[prevIndex];

            curEntry.SampleTimestamp = prevEntry.SampleTimestamp + 1;
            curEntry.SampleTimestamp2 = prevEntry.SampleTimestamp2 + 1;

            for (int i = 0; i < 8; ++i)
            {
                curEntry.Keys[i] = (uint)keyState.Keys[i];
            }

            curEntry.Modifier = (ulong)keyState.Modifier;
        }
    }
}