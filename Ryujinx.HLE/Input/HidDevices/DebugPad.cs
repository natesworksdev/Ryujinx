using static Ryujinx.HLE.Input.Hid;

namespace Ryujinx.HLE.Input
{
    public class DebugPad : BaseDevice
    {
        public DebugPad(Switch device, bool active) : base(device, active) { }

        public void Update()
        {
            ref var dpad = ref _device.Hid.SharedMemory.DebugPad;
            int prevIndex;

            int curIndex = UpdateEntriesHeader(ref dpad.Header, out prevIndex);

            if (!Active) return;

            ref var curEntry = ref dpad.Entries[curIndex];
            var prevEntry = dpad.Entries[prevIndex];

            curEntry.SequenceNumber = prevEntry.SequenceNumber + 1;
        }
    }
}