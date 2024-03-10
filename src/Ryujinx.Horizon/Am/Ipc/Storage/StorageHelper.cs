using LibHac.Fs;
using Ryujinx.Common.Memory;
using System.IO;

namespace Ryujinx.Horizon.Am.Ipc.Storage
{
    public class StorageHelper
    {
        private const uint LaunchParamsMagic = 0xc79497ca;

        public static byte[] MakeLaunchParams(UserId userId)
        {
            // Size needs to be at least 0x88 bytes otherwise application errors.
            using MemoryStream ms = MemoryStreamManager.Shared.GetStream();
            BinaryWriter writer = new(ms);

            ms.SetLength(0x88);

            writer.Write(LaunchParamsMagic);
            writer.Write(1);  // IsAccountSelected? Only lower 8 bits actually used.
            writer.Write(userId.AsBytes());

            return ms.ToArray();
        }
    }
}
