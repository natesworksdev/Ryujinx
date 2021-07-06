using Silk.NET.Vulkan;
using System;

namespace Ryujinx.Graphics.Vulkan
{
    class FormatCapabilities
    {
        private readonly FormatFeatureFlags[] _table;

        private readonly Vk _api;
        private readonly PhysicalDevice _physicalDevice;

        public FormatCapabilities(Vk api, PhysicalDevice physicalDevice)
        {
            _api = api;
            _physicalDevice = physicalDevice;
            _table = new FormatFeatureFlags[Enum.GetNames(typeof(GAL.Format)).Length];
        }

        public bool FormatSupports(GAL.Format format, FormatFeatureFlags flags)
        {
            var formatFeatureFlags = _table[(int)format];

            if (formatFeatureFlags == 0)
            {
                _api.GetPhysicalDeviceFormatProperties(_physicalDevice, FormatTable.GetFormat(format), out var fp);
                formatFeatureFlags = fp.OptimalTilingFeatures;
                _table[(int)format] = formatFeatureFlags;
            }

            return (formatFeatureFlags & flags) == flags;
        }
    }
}
