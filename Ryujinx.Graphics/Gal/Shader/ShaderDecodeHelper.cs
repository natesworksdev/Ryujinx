namespace Ryujinx.Graphics.Gal.Shader
{
    static class ShaderDecodeHelper
    {
        public static ShaderIrOper[] GetAluOperANode_A(long OpCode)
        {
            int Abuf = (int)(OpCode >> 20) & 0x3ff;
            int Reg  = (int)(OpCode >> 39) & 0xff;
            int Size = (int)(OpCode >> 47) & 3;

            ShaderIrOper[] Opers = new ShaderIrOper[Size + 1];

            for (int Index = 0; Index <= Size; Index++)
            {
                Opers[Index] = new ShaderIrOperAbuf(Abuf, (Reg + Index) & 0xff);
            }

            return Opers;
        }

        public static ShaderIrOper GetAluOperANode_R(long OpCode)
        {
            return new ShaderIrOperReg((int)(OpCode >> 8) & 0xff);
        }

        public static ShaderIrOper GetAluOperBNode_RR(long OpCode)
        {
            return new ShaderIrOperReg((int)(OpCode >> 20) & 0xff);
        }

        public static ShaderIrOper GetAluOperBCNode_R(long OpCode)
        {
            return new ShaderIrOperReg((int)(OpCode >> 39) & 0xff);
        }

        public static ShaderIrOper GetAluOperBCNode_C(long OpCode)
        {
            return new ShaderIrOperCbuf(
                (int)(OpCode >> 34) & 0x1f,
                (int)(OpCode >> 20) & 0x3fff);
        }

        public static ShaderIrOper GetAluOperDNode(long OpCode)
        {
            return new ShaderIrOperReg((int)(OpCode >>  0) & 0xff);
        }

        public static ShaderIrOper GetAluOperBNode_Imm(long OpCode)
        {
            //TODO
            return new ShaderIrOper();
        }
    }
}