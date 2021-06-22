using Ryujinx.HLE;
using Ryujinx.HLE.HOS.Services.Hid;
using Ryujinx.HLE.HOS.Services.Hid.Types.SharedMemory.TouchScreen;
using System;

namespace Ryujinx.Input.HLE
{
    public class TouchScreenManager : IDisposable
    {
        private readonly IMouse _mouse;
        private Switch _device;

        public TouchScreenManager(IMouse mouse)
        {
            _mouse = mouse;
        }

        public void Initialize(Switch device)
        {
            _device = device;
        }

        public bool Update(bool isFocused, float aspectRatio = 0)
        {
            if (!isFocused)
            {
                _device.Hid.Touchscreen.Update();

                return false;
            }

            if (aspectRatio > 0)
            {
                var snapshot = IMouse.GetMouseStateSnapshot(_mouse);
                var touchPosition = IMouse.GetTouchPosition(snapshot.Position, _mouse.ClientSize, aspectRatio);

                TouchPoint currentPoint = new TouchPoint
                {
                    Attribute = TouchAttribute.Start,

                    X = (uint)touchPosition.X,
                    Y = (uint)touchPosition.Y,

                    // Placeholder values till more data is acquired
                    DiameterX = 10,
                    DiameterY = 10,
                    Angle = 90
                };

                _device.Hid.Touchscreen.Update(currentPoint);

                // FIXME: We simulate an end of tap here. We do need a better and more way accurate to handle this.
                currentPoint.Attribute = TouchAttribute.End;

                _device.Hid.Touchscreen.Update(currentPoint);

                return true;
            }

            return false;
        }

        public void Dispose() { }
    }
}