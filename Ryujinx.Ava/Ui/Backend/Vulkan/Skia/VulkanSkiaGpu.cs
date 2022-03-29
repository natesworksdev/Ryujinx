using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Skia;
using Ryujinx.Ava.Vulkan;
using Ryujinx.Ava.Vulkan.Surfaces;
using Avalonia.X11;
using Silk.NET.Vulkan;
using SkiaSharp;

namespace Ryujinx.Ava.Ui.Backend.Vulkan
{
    public class VulkanSkiaGpu : ISkiaGpu
    {
        private readonly VulkanPlatformInterface _vulkan;
        private readonly long? _maxResourceBytes;
        private GRContext _grContext;
        private GRVkBackendContext _grVkBackend;
        private bool _initialized;

        public GRContext GrContext { get => _grContext; set => _grContext = value; }

        public VulkanSkiaGpu(long? maxResourceBytes)
        {
            _vulkan = AvaloniaLocator.Current.GetService<VulkanPlatformInterface>();
            _maxResourceBytes = maxResourceBytes;
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            GRVkGetProcedureAddressDelegate getProc = (string name, IntPtr instanceHandle, IntPtr deviceHandle) =>
            {
                IntPtr addr = IntPtr.Zero;

                if (deviceHandle != IntPtr.Zero)
                {
                    addr = _vulkan.Device.Api.GetDeviceProcAddr(new Device(deviceHandle), name);
                    if (addr != IntPtr.Zero)
                        return addr;

                    addr = _vulkan.Device.Api.GetDeviceProcAddr(new Device(_vulkan.Device.Handle), name);

                    if (addr != IntPtr.Zero)
                        return addr;
                }

                addr = _vulkan.Device.Api.GetInstanceProcAddr(new Instance(_vulkan.Instance.Handle), name);


                if (addr == IntPtr.Zero)
                    addr = _vulkan.Device.Api.GetInstanceProcAddr(new Instance(instanceHandle), name);

                return addr;
            };
            
            _grVkBackend = new GRVkBackendContext()
            {
                VkInstance = _vulkan.Device.Handle,
                VkPhysicalDevice = _vulkan.PhysicalDevice.Handle,
                VkDevice = _vulkan.Device.Handle,
                VkQueue = _vulkan.Device.Queue.Handle,
                GraphicsQueueIndex = _vulkan.PhysicalDevice.QueueFamilyIndex,
                GetProcedureAddress = getProc
            };
            _grContext = GRContext.CreateVulkan(_grVkBackend);
            if (_maxResourceBytes.HasValue)
            {
                _grContext.SetResourceCacheLimit(_maxResourceBytes.Value);
            }
        }

        public ISkiaGpuRenderTarget TryCreateRenderTarget(IEnumerable<object> surfaces)
        {
            foreach (var surface in surfaces)
            {
                VulkanWindowSurface window = null;

                if (surface is IPlatformHandle handle)
                {
                    window = new VulkanWindowSurface(handle.Handle);
                }
                else if (surface is X11FramebufferSurface x11FramebufferSurface)
                {
                    var xId = (IntPtr)x11FramebufferSurface.GetType().GetField("_xid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(x11FramebufferSurface);

                    window = new VulkanWindowSurface(xId);
                }

                if (window == null)
                {
                    return null;
                }

                var vulkanRenderTarget = new VulkanRenderTarget(_vulkan, window);

                Initialize();

                vulkanRenderTarget.GrContext = _grContext;

                return vulkanRenderTarget;
            }
            return null;
        }

        public ISkiaSurface TryCreateSurface(PixelSize size, ISkiaGpuRenderSession session)
        {
            return null;
        }

    }
}
