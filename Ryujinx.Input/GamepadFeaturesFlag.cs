using System;

namespace Ryujinx.Input
{
    /// <summary>
    /// Represent features supported by a <see cref="IGamepad"/>.
    /// </summary>
    [Flags]
    public enum GamepadFeaturesFlag
    {
        /// <summary>
        /// No features are supported.
        /// </summary>
        None,

        /// <summary>
        /// Rumble. (also named haptic)
        /// </summary>
        Rumble,

        /// <summary>
        /// Motion. (also named sixaxis)
        /// </summary>
        Motion
    }
}
