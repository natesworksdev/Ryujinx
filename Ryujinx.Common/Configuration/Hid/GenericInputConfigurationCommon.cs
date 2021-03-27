namespace Ryujinx.Common.Configuration.Hid
{
    public class GenericInputConfigurationCommon<Button, Stick> : InputConfig where Button : unmanaged where Stick : unmanaged
    {
        /// <summary>
        /// Left JoyCon Controller Bindings
        /// </summary>
        public LeftJoyconCommonConfig<Button, Stick> LeftJoycon { get; set; }

        /// <summary>
        /// Right JoyCon Controller Bindings
        /// </summary>
        public RightJoyconCommonConfig<Button, Stick> RightJoycon { get; set; }
    }
}
