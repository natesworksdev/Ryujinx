using Avalonia;
using Avalonia.Skia;
using Ryujinx.Ava.Vulkan;
using Ryujinx.Ava.Ui.Backend.OpenGl;
using Ryujinx.Ava.Ui.Backend.Vulkan;


namespace Ryujinx.Ava.Ui.Backend
{
    public static class SkiaGpuFactory
    {
        public static ISkiaGpu CreateOpenGlGpu()
        {
            var skiaOptions = AvaloniaLocator.Current.GetService<SkiaOptions>() ?? new SkiaOptions();
            var gpu = new OpenGlSkiaGpu(skiaOptions.MaxGpuResourceSizeBytes);
            AvaloniaLocator.CurrentMutable.Bind<OpenGlSkiaGpu>().ToConstant(gpu);

            return gpu;
        }
        public static ISkiaGpu CreateVulkanGpu()
        {
            var skiaOptions = AvaloniaLocator.Current.GetService<SkiaOptions>() ?? new SkiaOptions();
            var platformInterface = AvaloniaLocator.Current.GetService<VulkanPlatformInterface>();
            if(platformInterface == null) {
                VulkanPlatformInterface.TryInitialize();
            }
            var gpu = new VulkanSkiaGpu(skiaOptions.MaxGpuResourceSizeBytes);
            AvaloniaLocator.CurrentMutable.Bind<VulkanSkiaGpu>().ToConstant(gpu);
            return gpu;
        }
    }
}