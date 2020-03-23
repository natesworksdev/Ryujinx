
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Hid
{
    public class MouseDevice : BaseDevice
    {
        public MouseDevice(Switch device, bool active) : base(device, active)
        {
            if (Marshal.SizeOf<HidMouse>() != 0x400)
            {
                throw new System.DataMisalignedException($"HidMouse struct is the wrong size! Expected:0x400 Got:{Marshal.SizeOf<HidMouse>()}");
            }
        }

        public void Update(int mouseX, int mouseY, int buttons = 0, int scrollX = 0, int scrollY = 0)
        {
            ref HidMouse mouse = ref _device.Hid.SharedMemory.Mouse;
            int prevIndex;

            int curIndex = UpdateEntriesHeader(ref mouse.Header, out prevIndex);

            if (!Active) return;

            ref HidMouseEntry curEntry = ref mouse.Entries[curIndex];
            HidMouseEntry prevEntry = mouse.Entries[prevIndex];

            curEntry.SampleTimestamp = prevEntry.SampleTimestamp + 1;
            curEntry.SampleTimestamp2 = prevEntry.SampleTimestamp2 + 1;

            curEntry.Buttons = (ulong)buttons;

            curEntry.Position = new MousePosition
            {
                X = mouseX,
                Y = mouseY,
                VelocityX = mouseX - prevEntry.Position.X,
                VelocityY = mouseY - prevEntry.Position.Y,
                ScrollVelocityX = scrollX,
                ScrollVelocityY = scrollY
            };
        }
    }
}