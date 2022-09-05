using Ryujinx.Ava.Ui.Controls;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using SPB.Graphics.Vulkan;
using SPB.Platform.Win32;
using SPB.Platform.X11;
using SPB.Windowing;
using System;

namespace Ryujinx.Ava.Ui
{
    public class VulkanEmbeddedWindow : EmbeddedWindow
    {
        public SurfaceKHR CreateSurface(Instance instance)
        {
            NativeWindowBase window = null;
            if (OperatingSystem.IsWindows())
            {
                window = new SimpleWin32Window(new NativeHandle(WindowHandle));
            }
            else if (OperatingSystem.IsLinux())
            {
                window = new SimpleX11Window(new NativeHandle(X11Display), new NativeHandle(WindowHandle));
            }

            return new SurfaceKHR((ulong?)VulkanHelper.CreateWindowSurface(instance.Handle, window));
        }
    }
}