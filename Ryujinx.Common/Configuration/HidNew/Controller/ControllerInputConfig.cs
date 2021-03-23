namespace Ryujinx.Common.Configuration.HidNew.Controller
{
    public class ControllerInputConfig<Button, Stick> : ControllerInputConfigBase where Button : unmanaged where Stick : unmanaged
    {
        /// <summary>
        /// Left JoyCon Controller Bindings
        /// </summary>
        public LeftJoyconControllerConfig<Button, Stick> LeftJoycon;

        /// <summary>
        /// Right JoyCon Controller Bindings
        /// </summary>
        public RightJoyconControllerConfig<Button, Stick> RightJoycon;
    }
}
