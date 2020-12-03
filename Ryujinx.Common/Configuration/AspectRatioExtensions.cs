using System.Drawing;

namespace Ryujinx.Common.Configuration
{
    public enum AspectRatio
    {
        To43,
        To169,
        To219,
        To329,
        Stretched
    }

    public static class AspectRatioExtensions
    {
        public static float ToFloat(this AspectRatio aspectRatio)
        {
            return aspectRatio switch
            {
                AspectRatio.To43  => 4.0f  / 3.0f,
                AspectRatio.To169 => 16.0f / 9.0f,
                AspectRatio.To219 => 21.0f / 9.0f,
                AspectRatio.To329 => 32.0f / 9.0f,
                _                 => 16.0f / 9.0f
            };
        }

        public static string ToText(this AspectRatio aspectRatio)
        {
            return aspectRatio switch
            {
                AspectRatio.To43  => "4:3",
                AspectRatio.To169 => "16:9",
                AspectRatio.To219 => "21:9",
                AspectRatio.To329 => "32:9",
                _                 => "Stretched"
            };
        }
    }
}