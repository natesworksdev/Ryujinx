using ChocolArm64.Memory;
using Ryujinx.Graphics.Gal;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu
{
    class NvGpuEngine3d : INvGpuEngine
    {
        public int[] Registers { get; private set; }

        private NsGpu Gpu;

        private Dictionary<int, NvGpuMethod> Methods;

        private struct ConstBuffer
        {
            public bool Enabled;
            public long Position;
            public int  Size;
        }

        private ConstBuffer[] Cbs;

        public NvGpuEngine3d(NsGpu Gpu)
        {
            this.Gpu = Gpu;

            Registers = new int[0xe00];

            Methods = new Dictionary<int, NvGpuMethod>();

            void AddMethod(int Meth, int Count, int Stride, NvGpuMethod Method)
            {
                while (Count-- > 0)
                {
                    Methods.Add(Meth, Method);

                    Meth += Stride;
                }
            }

            AddMethod(0x585,  1, 1, VertexEndGl);
            AddMethod(0x6c3,  1, 1, QueryControl);
            AddMethod(0x8e4, 16, 1, CbData);
            AddMethod(0x904,  1, 1, CbBind);

            Cbs = new ConstBuffer[18];
        }

        public void CallMethod(AMemory Memory, NsGpuPBEntry PBEntry)
        {
            if (Methods.TryGetValue(PBEntry.Method, out NvGpuMethod Method))
            {
                Method(Memory, PBEntry);
            }
            else
            {
                WriteRegister(PBEntry);
            }
        }

        private void VertexEndGl(AMemory Memory, NsGpuPBEntry PBEntry)
        {
            int TexCbuf = ReadRegister(NvGpuEngine3dReg.TextureCbIndex);

            int TexHandle = ReadCb(Memory, TexCbuf, 0x20);

            long BasePosition = MakeInt64From2xInt32(NvGpuEngine3dReg.ShaderAddress);

            for (int Index = 0; Index < 6; Index++)
            {
                int Offset  = ReadRegister(NvGpuEngine3dReg.ShaderOffset + Index * 0x10);

                if (Offset == 0)
                {
                    continue;
                }

                long Position = Gpu.GetCpuAddr(BasePosition + (uint)Offset);

                if (Position == -1)
                {
                    continue;
                }

                //TODO: Find a better way to calculate the size.
                int Size = 0x20000;

                byte[] Code = AMemoryHelper.ReadBytes(Memory, Position, (uint)Size);

                Gpu.Renderer.CreateShader(Position, Code, GetTypeFromProgram(Index));
            }
        }

        private static GalShaderType GetTypeFromProgram(int Program)
        {
            switch (Program)
            {
                case 0:
                case 1: return GalShaderType.Vertex;
                case 2: return GalShaderType.TessControl;
                case 3: return GalShaderType.TessEvaluation;
                case 4: return GalShaderType.Geometry;
                case 5: return GalShaderType.Fragment;
            }

            throw new ArgumentOutOfRangeException(nameof(Program));
        }

        private void QueryControl(AMemory Memory, NsGpuPBEntry PBEntry)
        {
            if (TryGetCpuAddr(NvGpuEngine3dReg.QueryAddress, out long Position))
            {
                int Seq  = Registers[(int)NvGpuEngine3dReg.QuerySequence];
                int Ctrl = Registers[(int)NvGpuEngine3dReg.QueryControl];

                int Mode = Ctrl & 3;

                if (Mode == 0)
                {
                    //Write.
                    Memory.WriteInt32(Position, Seq);
                }
            }

            WriteRegister(PBEntry);
        }

        private void CbData(AMemory Memory, NsGpuPBEntry PBEntry)
        {
            if (TryGetCpuAddr(NvGpuEngine3dReg.CbAddress, out long Position))
            {
                int Offset = ReadRegister(NvGpuEngine3dReg.CbOffset);

                foreach (int Arg in PBEntry.Arguments)
                {
                    Memory.WriteInt32(Position + Offset, Arg);

                    Offset += 4;
                }

                WriteRegister(NvGpuEngine3dReg.CbOffset, Offset);
            }
        }

        private void CbBind(AMemory Memory, NsGpuPBEntry PBEntry)
        {
            int Index = PBEntry.Arguments[0];

            bool Enabled = (Index & 1) != 0;

            Index = (Index >> 4) & 0x1f;

            if (TryGetCpuAddr(NvGpuEngine3dReg.CbAddress, out long Position))
            {
                Cbs[Index].Position = Position;
            }

            Cbs[Index].Enabled = Enabled;
            Cbs[Index].Size    = ReadRegister(NvGpuEngine3dReg.CbSize);
        }

        private int ReadCb(AMemory Memory, int Cbuf, int Offset)
        {
            long Position = Cbs[Cbuf].Position;

            int Value = Memory.ReadInt32(Position + Offset);

            return Value;
        }

        private bool TryGetCpuAddr(NvGpuEngine3dReg Reg, out long Position)
        {
            Position = MakeInt64From2xInt32(Reg);

            Position = Gpu.GetCpuAddr(Position);

            return Position != -1;
        }

        private long MakeInt64From2xInt32(NvGpuEngine3dReg Reg)
        {
            return
                (long)Registers[(int)Reg + 0] << 32 |
                (uint)Registers[(int)Reg + 1];
        }

        private void WriteRegister(NsGpuPBEntry PBEntry)
        {
            int ArgsCount = PBEntry.Arguments.Count;

            if (ArgsCount > 0)
            {
                Registers[PBEntry.Method] = PBEntry.Arguments[ArgsCount - 1];
            }
        }

        private int ReadRegister(NvGpuEngine3dReg Reg)
        {
            return Registers[(int)Reg];
        }

        private void WriteRegister(NvGpuEngine3dReg Reg, int Value)
        {
            Registers[(int)Reg] = Value;
        }
    }
}