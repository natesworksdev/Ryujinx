using Ryujinx.Common.Configuration.Hid;
using System.Collections.Generic;

namespace Ryujinx.Common.Configuration.ConfigurationStateSection
{
    /// <summary>
    /// Hid configuration section
    /// </summary>
    public class HidSection
    {
        /// <summary>
        /// Enable or disable keyboard support (Independent from controllers binding)
        /// </summary>
        public ReactiveObject<bool> EnableKeyboard { get; }

        /// <summary>
        /// Hotkey Keyboard Bindings
        /// </summary>
        public ReactiveObject<KeyboardHotkeys> Hotkeys { get; }

        /// <summary>
        /// Input device configuration.
        /// NOTE: This ReactiveObject won't issue an event when the List has elements added or removed.
        /// TODO: Implement a ReactiveList class.
        /// </summary>
        public ReactiveObject<List<InputConfig>> InputConfig { get; }

        public HidSection()
        {
            EnableKeyboard = new ReactiveObject<bool>();
            Hotkeys = new ReactiveObject<KeyboardHotkeys>();
            InputConfig = new ReactiveObject<List<InputConfig>>();
        }
    }
}
