using System;

namespace Ryujinx.HLE.HOS.Services.Hid.HidServer
{
    static class HidUtils
    {
        public static PlayerIndex GetIndexFromNpadIdType(NpadIdType npadIdType)
        {
            switch (npadIdType)
            {
                case NpadIdType.Player1:  return PlayerIndex.Player1;
                case NpadIdType.Player2:  return PlayerIndex.Player2;
                case NpadIdType.Player3:  return PlayerIndex.Player3;
                case NpadIdType.Player4:  return PlayerIndex.Player4;
                case NpadIdType.Player5:  return PlayerIndex.Player5;
                case NpadIdType.Player6:  return PlayerIndex.Player6;
                case NpadIdType.Player7:  return PlayerIndex.Player7;
                case NpadIdType.Player8:  return PlayerIndex.Player8;
                case NpadIdType.Handheld: return PlayerIndex.Handheld;
                case NpadIdType.Unknown:  return PlayerIndex.Unknown;

                default: throw new ArgumentOutOfRangeException(nameof(npadIdType));
            }
        }

        public static NpadIdType GetNpadIdTypeFromIndex(PlayerIndex index)
        {
            switch (index)
            {
                case PlayerIndex.Player1:  return NpadIdType.Player1;
                case PlayerIndex.Player2:  return NpadIdType.Player2;
                case PlayerIndex.Player3:  return NpadIdType.Player3;
                case PlayerIndex.Player4:  return NpadIdType.Player4;
                case PlayerIndex.Player5:  return NpadIdType.Player5;
                case PlayerIndex.Player6:  return NpadIdType.Player6;
                case PlayerIndex.Player7:  return NpadIdType.Player7;
                case PlayerIndex.Player8:  return NpadIdType.Player8;
                case PlayerIndex.Handheld: return NpadIdType.Handheld;
                case PlayerIndex.Unknown:  return NpadIdType.Unknown;

                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}