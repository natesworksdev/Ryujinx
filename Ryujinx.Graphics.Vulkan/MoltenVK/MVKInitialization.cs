using Silk.NET.Vulkan;
using System;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Vulkan.MoltenVK
{
    [SupportedOSPlatform("macos")]
    public static class MVKInitialization
    {
        [DllImport("libMoltenVK.dylib")]
        private static extern Result vkGetMoltenVKConfigurationMVK(IntPtr unusedInstance, out MVKConfiguration config, in IntPtr configSize);

        [DllImport("libMoltenVK.dylib")]
        private static extern Result vkSetMoltenVKConfigurationMVK(IntPtr unusedInstance, in MVKConfiguration config, in IntPtr configSize);

        public static void Initialize()
        {
            var configSize = (IntPtr)Marshal.SizeOf<MVKConfiguration>();

            vkGetMoltenVKConfigurationMVK(IntPtr.Zero, out MVKConfiguration config, configSize);

            config.UseMetalArgumentBuffers = true;

            vkSetMoltenVKConfigurationMVK(IntPtr.Zero, config, configSize);
        }
    }
}