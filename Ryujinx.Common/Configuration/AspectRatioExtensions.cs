namespace Ryujinx.Common.Configuration
{
    public enum AspectRatio
    {
        To4x3,
        To16x9,
        To16x10,
        To21x9,
        To32x9,
        Stretched
    }

    public static class AspectRatioExtensions
    {
        public static float ToFloat(this AspectRatio aspectRatio)
        {
            return aspectRatio.ToFloatX() / aspectRatio.ToFloatY();
        }

        public static float ToFloatX(this AspectRatio aspectRatio)
        {
            return aspectRatio switch
            {
                AspectRatio.To4x3   => 4.0f,
                AspectRatio.To16x9  => 16.0f,
                AspectRatio.To16x10 => 16.0f,
                AspectRatio.To21x9  => 21.0f,
                AspectRatio.To32x9  => 32.0f,
                _                   => 16.0f
            };
        }

        public static float ToFloatY(this AspectRatio aspectRatio)
        {
            return aspectRatio switch
            {
                AspectRatio.To4x3   => 3.0f,
                AspectRatio.To16x9  => 9.0f,
                AspectRatio.To16x10 => 10.0f,
                AspectRatio.To21x9  => 9.0f,
                AspectRatio.To32x9  => 9.0f,
                _                   => 9.0f
            };
        }

        public static string ToText(this AspectRatio aspectRatio)
        {
            return aspectRatio switch
            {
                AspectRatio.To4x3   => "4:3",
                AspectRatio.To16x9  => "16:9",
                AspectRatio.To16x10 => "16:10",
                AspectRatio.To21x9  => "21:9",
                AspectRatio.To32x9  => "32:9",
                _                   => "Stretched"
            };
        }
    }
}