using System.Runtime.InteropServices;

namespace Ryujinx.HLE.OsHle.Services.Aud
{
    struct AudioRendererResponse
    {
        public int Revision;
        public int ErrorInfoSize;
        public int MemoryPoolsSize;
        public int VoicesSize;
        public int Unknown10;
        public int EffectsSize;
        public int Unknown18;
        public int SinksSize;
        public int PerformanceManagerSize;
        public int Padding0;
        public int Padding1;
        public int Padding2;
        public int Padding3;
        public int Padding4;
        public int Padding5;
        public int Padding6;
        public int TotalSize;

        AudioRendererResponse(AudioRendererConfig Config)
        {
            Revision = Config.Revision;
            ErrorInfoSize = 0xb0;
            MemoryPoolsSize = (Config.MemoryPoolsSize / 0x20) * 0x10;
            VoicesSize = (Config.VoicesSize / 0x170) * 0x10;
            EffectsSize = (Config.EffectsSize / 0xC0) * 0x10;
            SinksSize = (Config.SinksSize / 0x140) * 0x20;
            PerformanceManagerSize = 0x10;
            TotalSize = Marshal.SizeOf(AudioRendererResponse) + ErrorInfoSize + MemoryPoolsSize +
                         VoicesSize + EffectsSize + SinksSize + PerformanceManagerSize;
        }
    }
}
