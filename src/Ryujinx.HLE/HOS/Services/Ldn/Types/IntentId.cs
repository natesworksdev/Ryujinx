using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    struct IntentId
    {
        public ulong  LocalCommunicationId;
        public ushort Unknown1;
        public ushort SceneId;
        public uint   Unknown2;
    }
}