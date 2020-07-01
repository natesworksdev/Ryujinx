using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.RyuTas
{
    public class TASInstruction
    {
        public bool A { get; set; }
        public bool B { get; set; }
        public bool X { get; set; }
        public bool Y { get; set; }

        public bool Plus { get; set; }
        public bool Minus { get; set; }

        public bool DUp { get; set; }
        public bool DDown { get; set; }
        public bool DLeft { get; set; }
        public bool DRight { get; set; }

        public bool LStick { get; set; }
        public bool RStick { get; set; }

        public bool L { get; set; }
        public bool R { get; set; }
        public bool ZL { get; set; }
        public bool ZR { get; set; }

        public bool SlLeft { get; set; }
        public bool SrLeft { get; set; }
        public bool SlRight { get; set; }
        public bool SrRight { get; set; }

        public int LX { get; set; }
        public int LY { get; set; }

        public int RX { get; set; }
        public int RY { get; set; }

    }
}
