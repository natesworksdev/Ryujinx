using Ryujinx.HLE.HOS.Services.Account.Acc;

namespace Ryujinx.HLE.FileSystem
{
    readonly struct SaveInfo
    {
        public ulong        TitleId      { get; }
        public long         SaveId       { get; }
        public SaveDataType SaveDataType { get; }
        public SaveSpaceId  SaveSpaceId  { get; }
        public UserId       UserId       { get; }

        public SaveInfo(
            ulong        titleId,
            long         saveId,
            SaveDataType saveDataType,
            SaveSpaceId  saveSpaceId,
            UserId       userId = new UserId())
        {
            TitleId      = titleId;
            SaveId       = saveId;
            SaveDataType = saveDataType;
            SaveSpaceId  = saveSpaceId;
            UserId       = userId;
        }
    }
}