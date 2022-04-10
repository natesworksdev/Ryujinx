using Avalonia;
using Ryujinx.Ava.Vulkan;
using Ryujinx.Ava.Vulkan.Surfaces;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;

namespace Ryujinx.Ava.Ui.Backend.Vulkan
{
    public class VulkanWindowSurface : BackendSurface, IVulkanPlatformSurface
    {
        public float Scaling => (float)Program.WindowScaleFactor;

        public PixelSize SurfaceSize => Size;

        public VulkanWindowSurface(IntPtr handle) : base(handle)
        {
        }

        public unsafe SurfaceKHR CreateSurface(VulkanInstance instance)
        {
            if (OperatingSystem.IsWindows())
            {
                if (instance.Api.TryGetInstanceExtension(new Instance(instance.Handle), out KhrWin32Surface surfaceExtension))
                {
                    var createInfo = new Win32SurfaceCreateInfoKHR() { Hinstance = 0, Hwnd = Handle, SType = StructureType.Win32SurfaceCreateInfoKhr };

                    surfaceExtension.CreateWin32Surface(new Instance(instance.Handle), createInfo, null, out var surface).ThrowOnError();

                    return surface;
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                if (instance.Api.TryGetInstanceExtension(new Instance(instance.Handle), out KhrXlibSurface surfaceExtension))
                {
                    var createInfo = new XlibSurfaceCreateInfoKHR()
                    {
                        SType = StructureType.XlibSurfaceCreateInfoKhr,
                        Dpy = (nint*)Display.ToPointer(),
                        Window = Handle
                    };

                    surfaceExtension.CreateXlibSurface(new Instance(instance.Handle), createInfo, null, out var surface).ThrowOnError();

                    return surface;
                }
            }

            return new SurfaceKHR();
        }
    }
}