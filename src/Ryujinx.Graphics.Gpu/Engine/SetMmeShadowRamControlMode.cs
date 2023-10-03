namespace Ryujinx.Graphics.Gpu.Engine
{
    /// <summary>
    /// MME shadow RAM control mode.
    /// </summary>
    enum SetMmeShadowRamControlMode
    {
        MethodTrack = 0,
        MethodTrackWithFilter = 1,
        MethodPassthrough = 2,
        MethodReplay = 3,
    }

    static class SetMmeShadowRamControlModeExtensions
    {
        public static bool Track(this SetMmeShadowRamControlMode mode)
        {
            return mode == SetMmeShadowRamControlMode.MethodTrack || mode == SetMmeShadowRamControlMode.MethodTrackWithFilter;
        }

        public static bool Passthrough(this SetMmeShadowRamControlMode mode)
        {
            return mode == SetMmeShadowRamControlMode.MethodPassthrough;
        }

        public static bool Replay(this SetMmeShadowRamControlMode mode)
        {
            return mode == SetMmeShadowRamControlMode.MethodReplay;
        }
    }
}
