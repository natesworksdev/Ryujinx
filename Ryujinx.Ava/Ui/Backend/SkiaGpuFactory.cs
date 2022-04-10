using Avalonia;
using Avalonia.Skia;
using Ryujinx.Ava.Ui.Backend.OpenGl;


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
    }
}