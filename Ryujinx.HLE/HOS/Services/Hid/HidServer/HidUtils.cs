using System;

namespace Ryujinx.HLE.HOS.Services.Hid.HidServer
{
    static class HidUtils
    {
        public static HidControllerID GetIndexFromNpadIdType(HidNpadIdType npadIdType)
        {
            switch (npadIdType)
            {
                case HidNpadIdType.Player1:  return HidControllerID.Player1;
                case HidNpadIdType.Player2:  return HidControllerID.Player2;
                case HidNpadIdType.Player3:  return HidControllerID.Player3;
                case HidNpadIdType.Player4:  return HidControllerID.Player4;
                case HidNpadIdType.Player5:  return HidControllerID.Player5;
                case HidNpadIdType.Player6:  return HidControllerID.Player6;
                case HidNpadIdType.Player7:  return HidControllerID.Player7;
                case HidNpadIdType.Player8:  return HidControllerID.Player8;
                case HidNpadIdType.Handheld: return HidControllerID.Handheld;
                case HidNpadIdType.Unknown:  return HidControllerID.Unknown;

                default: throw new ArgumentOutOfRangeException(nameof(npadIdType));
            }
        }

        public static HidNpadIdType GetNpadIdTypeFromIndex(HidControllerID index)
        {
            switch (index)
            {
                case HidControllerID.Player1:  return HidNpadIdType.Player1;
                case HidControllerID.Player2:  return HidNpadIdType.Player2;
                case HidControllerID.Player3:  return HidNpadIdType.Player3;
                case HidControllerID.Player4:  return HidNpadIdType.Player4;
                case HidControllerID.Player5:  return HidNpadIdType.Player5;
                case HidControllerID.Player6:  return HidNpadIdType.Player6;
                case HidControllerID.Player7:  return HidNpadIdType.Player7;
                case HidControllerID.Player8:  return HidNpadIdType.Player8;
                case HidControllerID.Handheld: return HidNpadIdType.Handheld;
                case HidControllerID.Unknown:  return HidNpadIdType.Unknown;

                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}