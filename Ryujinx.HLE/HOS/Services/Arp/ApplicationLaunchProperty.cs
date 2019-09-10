using Ryujinx.HLE.FileSystem;

namespace Ryujinx.HLE.HOS.Services.Arp
{
    class ApplicationLaunchProperty
    {
        public long  TitleId;
        public int   Version;
        public byte  BaseGameStorageId;
        public byte  UpdateGameStorageId;
        public short Padding;

        public static ApplicationLaunchProperty Default
        {
            get
            {
                return new ApplicationLaunchProperty
                {
                    TitleId             = 0x00,
                    Version             = 0x00,
                    BaseGameStorageId   = (byte)StorageId.NandSystem,
                    UpdateGameStorageId = (byte)StorageId.None
                };
            }
        }
    }
}