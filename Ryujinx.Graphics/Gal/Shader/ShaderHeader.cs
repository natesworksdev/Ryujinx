namespace Ryujinx.Graphics.Gal.Shader
{
    class ShaderHeader
    {
        public const int PointList     = 1;
        public const int LineStrip     = 6;
        public const int TriangleStrip = 7;

        public int ShaderType { get; private set; }

        public int OutputTopology { get; private set; }

        public int MaxOutputVertexCount { get; private set; }

        public ShaderHeader(IGalMemory Memory, long Position)
        {
            uint CommonWord0 = (uint)Memory.ReadInt32(Position + 0);
            uint CommonWord1 = (uint)Memory.ReadInt32(Position + 4);
            uint CommonWord2 = (uint)Memory.ReadInt32(Position + 8);
            uint CommonWord3 = (uint)Memory.ReadInt32(Position + 12);
            uint CommonWord4 = (uint)Memory.ReadInt32(Position + 16);

            int  SphType         = ReadBits(CommonWord0,  0, 5);
            int  Version         = ReadBits(CommonWord0,  5, 5);
                 ShaderType      = ReadBits(CommonWord0, 10, 4);
            bool MrtEnable       = ReadBits(CommonWord0, 14, 1) != 0;
            bool KillsPixels     = ReadBits(CommonWord0, 15, 1) != 0;
            bool DoesGlobalStore = ReadBits(CommonWord0, 16, 1) != 0;
            int  SassVersion     = ReadBits(CommonWord0, 17, 4);
            bool DoesLoadOrStore = ReadBits(CommonWord0, 26, 1) != 0;
            bool DoesFp64        = ReadBits(CommonWord0, 27, 1) != 0;
            int  StreamOutMask   = ReadBits(CommonWord0, 28, 4);

            int ShaderLocalMemoryLowSize = ReadBits(CommonWord1,  0, 24);
            int PerPatchAttributeCount   = ReadBits(CommonWord1, 24,  8);

            int ShaderLocalMemoryHighSize = ReadBits(CommonWord2,  0, 24);
            int ThreadsPerInputPrimitive  = ReadBits(CommonWord2, 24,  8);

            int ShaderLocalMemoryCrsSize = ReadBits(CommonWord3,  0, 24);
                OutputTopology           = ReadBits(CommonWord3, 24,  4);

                MaxOutputVertexCount = ReadBits(CommonWord4,  0, 12);
            int StoreReqStart        = ReadBits(CommonWord4, 12,  8);
            int StoreReqEnd          = ReadBits(CommonWord4, 24,  8);
        }

        private static int ReadBits(uint Word, int Offset, int BitWidth)
        {
            uint Mask = (1u << BitWidth) - 1u;

            return (int)((Word >> Offset) & Mask);
        }
    }
}