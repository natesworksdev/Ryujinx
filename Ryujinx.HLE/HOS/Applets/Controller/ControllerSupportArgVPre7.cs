using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Applets
{
#pragma warning disable CS0649
    // (1.0.0+ version)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct ControllerSupportArgVPre7 : IControllerSupportArg
    {
        const int MaxPlayers = 4;
        const int TextSize = 0x81;

        public ControllerSupportArgHeader Header;
        public byte EnableIdentificationColor;
        public fixed uint IdentificationColor[MaxPlayers];
        public byte EnableExplainText;
        public fixed byte ExplainText[MaxPlayers * TextSize];

        public string[] GetExplainTexts()
        {
            if (EnableExplainText == 0)
            {
                return new string[0];
            }

            string[] texts = new string[MaxPlayers];
            fixed (byte* textPtr = ExplainText)
            {
                for (int i = 0; i < MaxPlayers; ++i)
                {
                    texts[i] = Marshal.PtrToStringUTF8((IntPtr)textPtr + i * TextSize);
                }
            }

            return texts;
        }

        public uint[] GetIdentificationColors()
        {
            if (EnableIdentificationColor == 0)
            {
                return new uint[0];
            }

            uint[] colors = new uint[MaxPlayers];
            for (int i = 0; i < MaxPlayers; ++i)
            {
                colors[i] = IdentificationColor[i];
            }

            return colors;
        }
    }
#pragma warning restore CS0649
}