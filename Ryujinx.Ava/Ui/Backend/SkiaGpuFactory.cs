using Avalonia;
using Avalonia.Skia;
using Ryujinx.Ava.Ui.Backend.OpenGL;

namespace Ryujinx.Ava.Ui.Backend
{
    public static class SkiaGpuFactory
    {
        public static ISkiaGpu CreateOpenGlGpu()
        {
            var skiaOptions = AvaloniaLocator.Current.GetService<SkiaOptions>() ?? new SkiaOptions();
            var gpu = new OpenGLSkiaGpu(skiaOptions.MaxGpuResourceSizeBytes);
            AvaloniaLocator.CurrentMutable.Bind<OpenGLSkiaGpu>().ToConstant(gpu);

            return gpu;
        }
    }
}