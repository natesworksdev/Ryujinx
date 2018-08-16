using System;
using System.IO;

using Ryujinx.HLE.Exceptions;

namespace Ryujinx.HLE.Loaders.Npdm
{
    public class FsAccessHeader
    {
        public int   Version            { get; private set; }
        public ulong PermissionsBitmask { get; private set; }

        public FsAccessHeader(Stream Stream, int Offset, int Size)
        {
            Stream.Seek(Offset, SeekOrigin.Begin);

            BinaryReader Reader = new BinaryReader(Stream);

            Version            = Reader.ReadInt32();
            PermissionsBitmask = Reader.ReadUInt64();

            int DataSize = Reader.ReadInt32();

            if (DataSize != 0x1c)
            {
                throw new InvalidNpdmException("FsAccessHeader is corrupted!");
            }

            int ContentOwnerIdSize        = Reader.ReadInt32();
            int DataAndContentOwnerIdSize = Reader.ReadInt32();

            if (DataAndContentOwnerIdSize != 0x1c)
            {
                throw new NotImplementedException("ContentOwnerId section is not implemented!");
            }
        }
    }
}
