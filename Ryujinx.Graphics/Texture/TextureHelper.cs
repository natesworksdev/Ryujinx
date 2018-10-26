using ChocolArm64.Memory;
using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Memory;

namespace Ryujinx.Graphics.Texture
{
    internal static class TextureHelper
    {
        public static ISwizzle GetSwizzle(GalImage image)
        {
            int blockWidth    = ImageUtils.GetBlockWidth   (image.Format);
            int bytesPerPixel = ImageUtils.GetBytesPerPixel(image.Format);

            int width = (image.Width + (blockWidth - 1)) / blockWidth;

            if (image.Layout == GalMemoryLayout.BlockLinear)
            {
                int alignMask = image.TileWidth * (64 / bytesPerPixel) - 1;

                width = (width + alignMask) & ~alignMask;

                return new BlockLinearSwizzle(width, bytesPerPixel, image.GobBlockHeight);
            }
            else
            {
                return new LinearSwizzle(image.Pitch, bytesPerPixel);
            }
        }

        public static (AMemory Memory, long Position) GetMemoryAndPosition(
            IAMemory memory,
            long     position)
        {
            if (memory is NvGpuVmm vmm) return (vmm.Memory, vmm.GetPhysicalAddress(position));

            return ((AMemory)memory, position);
        }
    }
}
