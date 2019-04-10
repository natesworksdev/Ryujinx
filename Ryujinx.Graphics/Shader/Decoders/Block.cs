using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.Decoders
{
    class Block
    {
        public int Index { get; set; }

        public ulong Address    { get; set; }
        public ulong EndAddress { get; set; }

        public Block Next   { get; set; }
        public Block Branch { get; set; }

        public List<OpCode> OpCodes { get; }

        public List<OpCodeSsy> SsyOpCodes { get; }

        public Block(ulong address)
        {
            Address = address;

            OpCodes = new List<OpCode>();

            SsyOpCodes = new List<OpCodeSsy>();
        }

        public OpCode GetLastOp()
        {
            if (OpCodes.Count > 0)
            {
                return OpCodes[OpCodes.Count - 1];
            }

            return null;
        }

        public void UpdateSsyOpCodes()
        {
            SsyOpCodes.Clear();

            for (int index = 0; index < OpCodes.Count; index++)
            {
                if (!(OpCodes[index] is OpCodeSsy op))
                {
                    continue;
                }

                SsyOpCodes.Add(op);
            }
        }
    }
}