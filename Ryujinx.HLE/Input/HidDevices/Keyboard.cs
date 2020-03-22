using static Ryujinx.HLE.Input.Hid;

namespace Ryujinx.HLE.Input
{

    public struct Keyboard
    {
        public int Modifier;
        public int[] Keys;
    }

    public class KeyboardDevice : BaseDevice
    {
        public KeyboardDevice(Switch device, bool active) : base(device, active) { }

        public unsafe void Update(Keyboard keyState)
        {
            ref var keyboard = ref _device.Hid.SharedMemory.Keyboard;

            int prevIndex;
            int curIndex = UpdateEntriesHeader(ref keyboard.Header, out prevIndex);

            if (!Active) return;

            ref var curEntry = ref keyboard.Entries[curIndex];
            var prevEntry = keyboard.Entries[prevIndex];

            curEntry.SequenceNumber = prevEntry.SequenceNumber + 1;
            curEntry.SequenceNumber2 = prevEntry.SequenceNumber2 + 1;

            for (int i = 0; i < 8; ++i)
            {
                curEntry.Keys[i] = (uint)keyState.Keys[i];
            }
            curEntry.Modifier = (ulong)keyState.Modifier;
        }
    }
}