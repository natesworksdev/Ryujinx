using System.Collections.Generic;

namespace ChocolArm64.Decoder
{
    class Block
    {
        public long Position    { get; set; }
        public long EndPosition { get; set; }

        public Block Next   { get; set; }
        public Block Branch { get; set; }

        public List<AOpCode> OpCodes { get; private set; }

        public Block()
        {
            OpCodes = new List<AOpCode>();
        }

        public Block(long position) : this()
        {
            Position = position;
        }

        public AOpCode GetLastOp()
        {
            if (OpCodes.Count > 0)
            {
                return OpCodes[OpCodes.Count - 1];
            }

            return null;
        }
    }
}