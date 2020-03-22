using static Ryujinx.HLE.Input.Hid;

namespace Ryujinx.HLE.Input
{
    public class MouseDevice : BaseDevice
    {
        public MouseDevice(Switch device, bool active) : base(device, active) { }

        public void Update(int mouseX, int mouseY, int buttons = 0, int scrollX = 0, int scrollY = 0)
        {
            ref var mouse = ref _device.Hid.SharedMemory.Mouse;
            int prevIndex;

            int curIndex = UpdateEntriesHeader(ref mouse.Header, out prevIndex);

            if (!Active) return;

            ref var curEntry = ref mouse.Entries[curIndex];
            var prevEntry = mouse.Entries[prevIndex];

            curEntry.SequenceNumber = prevEntry.SequenceNumber + 1;
            curEntry.SequenceNumber2 = prevEntry.SequenceNumber2 + 1;

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