namespace Ryujinx.HLE.HOS.Services.Hid
{
    public class DebugPadDevice : BaseDevice
    {
        public DebugPadDevice(Switch device, bool active) : base(device, active) { }

        public void Update()
        {
            using var hidMemory = _device.System.ServiceServer.HidServer.GetSharedMemory();

            ref ShMemDebugPad debugPad = ref hidMemory.GetRef<HidSharedMemory>(0).DebugPad;

            int currentIndex = UpdateEntriesHeader(ref debugPad.Header, out int previousIndex);

            if (!Active)
            {
                return;
            }

            ref DebugPadEntry currentEntry = ref debugPad.Entries[currentIndex];
            DebugPadEntry previousEntry = debugPad.Entries[previousIndex];

            currentEntry.SampleTimestamp = previousEntry.SampleTimestamp + 1;
        }
    }
}