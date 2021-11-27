using Ryujinx.Common.Memory;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Shader
{
    public struct Vector4<T>
    {
        public T X;
        public T Y;
        public T Z;
        public T W;
    }

    public struct SupportBuffer
    {
        public static int FieldSize;
        public static int RequiredSize;

        public static int FragmentAlphaTestOffset;
        public static int FragmentIsBgraOffset;
        public static int FragmentRenderScaleCountOffset;
        public static int GraphicsRenderScaleOffset;
        public static int ComputeRenderScaleOffset;

        public const int FragmentIsBgraCount = 8;
        // One for the render target, 32 for the textures, and 8 for the images.
        public const int RenderScaleMaxCount = 1 + 32 + 8;

        static SupportBuffer()
        {
            FieldSize = Unsafe.SizeOf<Vector4<float>>();
            RequiredSize = Unsafe.SizeOf<SupportBuffer>();

            FragmentAlphaTestOffset = (int)Marshal.OffsetOf<SupportBuffer>(nameof(FragmentAlphaTest));
            FragmentIsBgraOffset = (int)Marshal.OffsetOf<SupportBuffer>(nameof(FragmentIsBgra));
            FragmentRenderScaleCountOffset = (int)Marshal.OffsetOf<SupportBuffer>(nameof(FragmentRenderScaleCount));
            GraphicsRenderScaleOffset = (int)Marshal.OffsetOf<SupportBuffer>(nameof(RenderScale));
            ComputeRenderScaleOffset = GraphicsRenderScaleOffset + FieldSize;
        }

        public Vector4<int> FragmentAlphaTest;
        public Array8<Vector4<int>> FragmentIsBgra;
        public Vector4<int> FragmentRenderScaleCount;

        // Render scale max count: 1 + 32 + 8. First scale is fragment output scale, others are textures/image inputs.
        public Array41<Vector4<float>> RenderScale;
    }
}