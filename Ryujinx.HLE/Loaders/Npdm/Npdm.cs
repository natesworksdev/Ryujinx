using Ryujinx.HLE.Exceptions;
using Ryujinx.HLE.Utilities;
using System.IO;
using System.Text;

namespace Ryujinx.HLE.Loaders.Npdm
{
    //https://github.com/SciresM/hactool/blob/master/npdm.c
    //https://github.com/SciresM/hactool/blob/master/npdm.h
    //http://switchbrew.org/index.php?title=NPDM
    class Npdm
    {
        private const int MetaMagic = 'M' << 0 | 'E' << 8 | 'T' << 16 | 'A' << 24;

        public bool   Is64Bits                { get; private set; }
        public int    AddressSpaceWidth       { get; private set; }
        public byte   MainThreadPriority      { get; private set; }
        public byte   DefaultCpuId            { get; private set; }
        public int    SystemResourceSize      { get; private set; }
        public int    ProcessCategory         { get; private set; }
        public int    MainEntrypointStackSize { get; private set; }
        public string TitleName               { get; private set; }
        public byte[] ProductCode             { get; private set; }

        public Aci0 Aci0 { get; private set; }
        public Acid Acid { get; private set; }

        public Npdm(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            if (reader.ReadInt32() != MetaMagic)
            {
                throw new InvalidNpdmException("NPDM Stream doesn't contain NPDM file!");
            }

            reader.ReadInt64();

            //MmuFlags, bit0: 64-bit instructions, bits1-3: address space width (1=64-bit, 2=32-bit). Needs to be <= 0xF.
            byte mmuFlags = reader.ReadByte();

            Is64Bits          = (mmuFlags & 1) != 0;
            AddressSpaceWidth = (mmuFlags >> 1) & 7;

            reader.ReadByte();

            MainThreadPriority = reader.ReadByte(); //(0-63).
            DefaultCpuId       = reader.ReadByte();

            reader.ReadInt32();

            //System resource size (max size as of 5.x: 534773760).
            SystemResourceSize = EndianSwap.Swap32(reader.ReadInt32());

            //ProcessCategory (0: regular title, 1: kernel built-in). Should be 0 here.
            ProcessCategory = EndianSwap.Swap32(reader.ReadInt32());

            //Main entrypoint stack size.
            MainEntrypointStackSize = reader.ReadInt32();

            byte[] tempTitleName = reader.ReadBytes(0x10);

            TitleName = Encoding.UTF8.GetString(tempTitleName, 0, tempTitleName.Length).Trim('\0');

            ProductCode = reader.ReadBytes(0x10);

            stream.Seek(0x30, SeekOrigin.Current);

            int aci0Offset = reader.ReadInt32();
            int aci0Size   = reader.ReadInt32();
            int acidOffset = reader.ReadInt32();
            int acidSize   = reader.ReadInt32();

            Aci0 = new Aci0(stream, aci0Offset);
            Acid = new Acid(stream, acidOffset);
        }
    }
}
