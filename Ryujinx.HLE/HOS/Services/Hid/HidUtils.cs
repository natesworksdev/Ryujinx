using System;

namespace Ryujinx.HLE.HOS.Services.Hid
{
    static class HidUtils
    {
        public static int GetNpadTypeId(uint npadId)
        {
            switch (npadId)
            {
                case 0:  return 0;
                case 1:  return 1;
                case 2:  return 2;
                case 3:  return 3;
                case 4:  return 4;
                case 5:  return 5;
                case 6:  return 6;
                case 7:  return 7;
                case 32: return 8;
                case 16: return 9;
                default: throw new ArgumentOutOfRangeException(nameof(npadId));
            }
        }
    }
}