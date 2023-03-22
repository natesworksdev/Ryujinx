using System;
using static Ryujinx.Ui.Common.Configuration.ConfigurationState.UiSection;
using Ryujinx.Common;

namespace Ryujinx.Ui.Common.Configuration
{
    public enum FileTypes
    {
        NSP,
        PFS0,
        XCI,
        NCA,
        NRO,
        NSO
    }

    public static class FileTypesExtensions
    {
        /// <summary>
        /// Gets the current <see cref="ShownFileTypeSettings"/> value for the correlating FileType name.
        /// </summary>
        /// <param name="type">The name of the <see cref="ShownFileTypeSettings"/> parameter to get the value of.</param>
        /// <param name="config">The config instance to get the value from.</param>
        /// <returns>The current value of the setting. Value is <see langword="true"/> if the file type is the be shown on the games list, <see langword="false"/> otherwise.</returns>
        public static bool GetConfigValue(this FileTypes type, ShownFileTypeSettings config)
        {
            return ((ReactiveObject<bool>) typeof(ShownFileTypeSettings).GetProperty(Enum.GetName(type))
                .GetValue(config, null)).Value;
        }
    }
}
