using Ryujinx.HLE.HOS.Services.Account.Acc;

namespace Ryujinx.HLE.FileSystem
{
    readonly struct SaveInfo
    {
        public readonly ulong TitleId;
        public readonly long SaveId;
        public readonly SaveDataType SaveDataType;
        public readonly SaveSpaceId SaveSpaceId;
        public readonly UserId UserId;

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