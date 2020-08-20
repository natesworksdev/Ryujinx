namespace Ryujinx.HLE.HOS.Services.Hid
{
    public class KeyboardDevice : BaseDevice
    {
        public KeyboardDevice(Switch device, bool active) : base(device, active) { }

        public unsafe void Update(KeyboardInput keyState)
        {
            using var hidMemory = _device.System.ServiceServer.HidServer.GetSharedMemory();

            ref ShMemKeyboard keyboard = ref hidMemory.GetRef<HidSharedMemory>(0).Keyboard;

            int currentIndex = UpdateEntriesHeader(ref keyboard.Header, out int previousIndex);

            if (!Active)
            {
                return;
            }

            ref KeyboardState currentEntry = ref keyboard.Entries[currentIndex];
            KeyboardState previousEntry = keyboard.Entries[previousIndex];

            currentEntry.SampleTimestamp = previousEntry.SampleTimestamp + 1;
            currentEntry.SampleTimestamp2 = previousEntry.SampleTimestamp2 + 1;

            for (int i = 0; i < 8; ++i)
            {
                currentEntry.Keys[i] = (uint)keyState.Keys[i];
            }

            currentEntry.Modifier = (ulong)keyState.Modifier;
        }
    }
}