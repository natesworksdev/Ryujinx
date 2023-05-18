using Ryujinx.Common.Memory;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.Common.Utilities
{
    public static class StreamUtils
    {
        public static byte[] StreamToBytes(Stream input)
        {
            using (MemoryStream stream = MemoryStreamManager.Shared.GetStream())
            {
                input.CopyTo(stream);

                return stream.ToArray();
            }
        }

        public static IMemoryOwner<byte> StreamToOwnedMemory(Stream input)
        {
            long bytesExpected = input.Length;

            IMemoryOwner<byte> ownedMemory = ByteMemoryPool.Shared.Rent(bytesExpected);

            int bytesRead = input.Read(ownedMemory.Memory.Span);

            if (bytesRead != bytesExpected)
            {
                throw new IOException($"Read {bytesRead} but expected to read {bytesExpected}.");
            }

            return ownedMemory;
        }

        public static async Task<byte[]> StreamToBytesAsync(Stream input, CancellationToken cancellationToken = default)
        {
            using (MemoryStream stream = MemoryStreamManager.Shared.GetStream())
            {
                await input.CopyToAsync(stream, cancellationToken);

                return stream.ToArray();
            }
        }
    }
}