using Ryujinx.HLE.OsHle.Utilities;
using System;
using System.IO;

namespace Ryujinx.HLE.Loaders.Npdm
{
    public class ACI0
    {
        public string TitleId;

        private int   FSAccessHeaderOffset;
        private int   FSAccessHeaderSize;
        private int   ServiceAccessControlOffset;
        private int   ServiceAccessControlSize;
        private int   KernelAccessControlOffset;
        private int   KernelAccessControlSize;

        public FSAccessHeader       FSAccessHeader;
        public ServiceAccessControl ServiceAccessControl;
        public KernelAccessControl  KernelAccessControl;

        public ACI0(Stream ACI0Stream, int Offset)
        {
            ACI0Stream.Seek(Offset, SeekOrigin.Begin);

            BinaryReader Reader = new BinaryReader(ACI0Stream);

            if (EndianSwap.Swap32(Reader.ReadInt32()) != 0x41434930) // ACI0
            {
                throw new InvalidNpdmException("ACI0 Stream doesn't contain ACI0 section!");
            }

            ACI0Stream.Seek(0x0C, SeekOrigin.Current);

            byte[] TempTitleId = Reader.ReadBytes(8);
            Array.Reverse(TempTitleId);
            TitleId = BitConverter.ToString(TempTitleId).Replace("-", "");

            // Reserved (Not currently used, potentially to be used for lowest title ID in future.)
            ACI0Stream.Seek(0x08, SeekOrigin.Current);

            FSAccessHeaderOffset       = Reader.ReadInt32();
            FSAccessHeaderSize         = Reader.ReadInt32();
            ServiceAccessControlOffset = Reader.ReadInt32();
            ServiceAccessControlSize   = Reader.ReadInt32();
            KernelAccessControlOffset  = Reader.ReadInt32();
            KernelAccessControlSize    = Reader.ReadInt32();

            FSAccessHeader       = new FSAccessHeader(ACI0Stream, Offset + FSAccessHeaderOffset, FSAccessHeaderSize);
            ServiceAccessControl = new ServiceAccessControl(ACI0Stream, Offset + ServiceAccessControlOffset, ServiceAccessControlSize);
            KernelAccessControl  = new KernelAccessControl(ACI0Stream, Offset + KernelAccessControlOffset, KernelAccessControlSize);
        }
    }
}
