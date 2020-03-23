using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Hid
{
    public class DebugPad : BaseDevice
    {
        public DebugPad(Switch device, bool active) : base(device, active)
        {
            if (Marshal.SizeOf<HidDebugPad>() != 0x400)
            {
                throw new System.DataMisalignedException($"HidDebugPad struct is the wrong size! Expected:0x400 Got:{Marshal.SizeOf<HidDebugPad>()}");
            }
        }

        public void Update()
        {
            ref HidDebugPad dpad = ref _device.Hid.SharedMemory.DebugPad;
            int prevIndex;

            int curIndex = UpdateEntriesHeader(ref dpad.Header, out prevIndex);

            if (!Active) return;

            ref HidDebugPadEntry curEntry = ref dpad.Entries[curIndex];
            HidDebugPadEntry prevEntry = dpad.Entries[prevIndex];

            curEntry.SampleTimestamp = prevEntry.SampleTimestamp + 1;
        }
    }
}