namespace Ryujinx.Common.Configuration.HidNew.Keyboard
{
    public class GenericKeyboardInputConfig<Key> : GenericInputConfigurationCommon<Key, Key> where Key : unmanaged
    {
        /// <summary>
        /// Left JoyCon Controller Stick Bindings
        /// </summary>
        public JoyconConfigKeyboardStick<Key> LeftJoyconStick;

        /// <summary>
        /// Right JoyCon Controller Stick Bindings
        /// </summary>
        public JoyconConfigKeyboardStick<Key> RightJoyconStick;
    }
}
