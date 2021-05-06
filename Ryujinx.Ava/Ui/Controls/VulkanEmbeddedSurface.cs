using OpenTK.Windowing.GraphicsLibraryFramework;
using Silk.NET.Vulkan;

namespace Ryujinx.Ava.Ui.Controls
{
    public class VulkanEmbeddedSurface : NativeEmbeddedWindow
    {
        public VulkanEmbeddedSurface(double scale) : base(scale) { }

        public unsafe SurfaceKHR CreateSurface(Instance instance, Vk vk)
        {
            GLFW.CreateWindowSurface(new VkHandle(instance.Handle), GlfwWindow.WindowPtr, null, out VkHandle surface);

            return new SurfaceKHR((ulong)surface.Handle.ToInt64());
        }

        public string[] GetRequiredInstanceExtensions()
        {
            return GLFW.GetRequiredInstanceExtensions();
        }
    }
}