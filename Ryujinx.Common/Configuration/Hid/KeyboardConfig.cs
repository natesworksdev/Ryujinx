using Ryujinx.Configuration.Hid;

namespace Ryujinx.Common.Configuration.Hid
{
    public class KeyboardConfig : InputConfig
    {
        // DO NOT MODIFY
        public const uint AllKeyboardsIndex = 0;

        /// <summary>
        /// Controller Left Analog Stick Range Modifier Key
        /// </summary>
        public Key LeftStickRangeButton { get; set; }

        /// <summary>
        /// Controller Right Analog Stick Range Modifier Key
        /// </summary>
        public Key RightStickRangeButton { get; set; }

        /// <summary>
        /// Left JoyCon Keyboard Bindings
        /// </summary>
        public NpadKeyboardLeft LeftJoycon { get; set; }

        /// <summary>
        /// Right JoyCon Keyboard Bindings
        /// </summary>
        public NpadKeyboardRight RightJoycon { get; set; }
    }
}