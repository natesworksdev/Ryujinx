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
            AddMethod(0x674,  1, 1, ClearBuffers);
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

            UploadShaders(Memory);

            Gpu.Renderer.BindProgram();

            UploadUniforms(Memory);
            UploadVertexArrays(Memory);
        }

        private void ClearBuffers(AMemory Memory, NsGpuPBEntry PBEntry)
        {
            int Arg0 = PBEntry.Arguments[0];

            int Rt = (Arg0 >> 6) & 0xf;

            GalClearBufferFlags Flags = (GalClearBufferFlags)(Arg0 & 0x3f);

            Gpu.Renderer.ClearBuffers(Rt, Flags);
        }

        private void UploadShaders(AMemory Memory)
        {
            long BasePosition = MakeInt64From2xInt32(NvGpuEngine3dReg.ShaderAddress);

            for (int Index = 0; Index < 6; Index++)
            {
                int Control = ReadRegister(NvGpuEngine3dReg.ShaderNControl + Index * 0x10);
                int Offset  = ReadRegister(NvGpuEngine3dReg.ShaderNOffset  + Index * 0x10);

                if (Offset == 0 || (Index != 1 && Index != 5))
                {
                    continue;
                }

                long Tag = BasePosition + (uint)Offset;

                long Position = Gpu.GetCpuAddr(Tag);

                if (Position == -1)
                {
                    continue;
                }

                //TODO: Find a better way to calculate the size.
                int Size = 0x20000;

                byte[] Code = AMemoryHelper.ReadBytes(Memory, Position, (uint)Size);

                GalShaderType ShaderType = GetTypeFromProgram(Index);

                Gpu.Renderer.CreateShader(Tag, ShaderType, Code);
                Gpu.Renderer.BindShader(Tag);
            }
        }

        private void UploadUniforms(AMemory Memory)
        {
            long BasePosition = MakeInt64From2xInt32(NvGpuEngine3dReg.ShaderAddress);

            for (int Index = 0; Index < 5; Index++)
            {
                int Offset = ReadRegister(NvGpuEngine3dReg.ShaderNOffset + (Index + 1) * 0x10);

                long Tag = BasePosition + (uint)Offset;

                if (Offset == 0 || (Index != 0 && Index != 4))
                {
                    continue;
                }

                for (int Cbuf = 0; Cbuf < Cbs.Length; Cbuf++)
                {
                    ConstBuffer Cb = Cbs[Cbuf];

                    if (Cb.Enabled)
                    {
                        long CbPosition = Cb.Position + Index * Cb.Size;

                        byte[] Data = AMemoryHelper.ReadBytes(Memory, CbPosition, (uint)Cb.Size);

                        Gpu.Renderer.SetShaderCb(Tag, Cbuf, Data);
                    }
                }
            }
        }

        private void UploadVertexArrays(AMemory Memory)
        {
            List<GalVertexAttrib>[] Attribs = new List<GalVertexAttrib>[32];

            for (int Attr = 0; Attr < 16; Attr++)
            {
                int Packed = ReadRegister(NvGpuEngine3dReg.VertexAttribNFormat + Attr);

                int ArrayIndex = Packed & 0x1f;

                if (Attribs[ArrayIndex] == null)
                {
                    Attribs[ArrayIndex] = new List<GalVertexAttrib>();
                }

                Attribs[ArrayIndex].Add(new GalVertexAttrib(
                                         ((Packed >>  6) & 0x1) != 0,
                                          (Packed >>  7) & 0x3fff,
                    (GalVertexAttribSize)((Packed >> 21) & 0x3f),
                    (GalVertexAttribType)((Packed >> 27) & 0x7),
                                         ((Packed >> 31) & 0x1) != 0));
            }

            for (int Index = 0; Index < 32; Index++)
            {
                int Control = ReadRegister(NvGpuEngine3dReg.VertexArrayNControl + Index * 4);

                bool Enable = (Control & 0x1000) != 0;

                if (!Enable)
                {
                    continue;
                }

                long Position = MakeInt64From2xInt32(NvGpuEngine3dReg.VertexArrayNAddress + Index * 4);
                long EndPos   = MakeInt64From2xInt32(NvGpuEngine3dReg.VertexArrayNEndAddr + Index * 4);

                long Size = (EndPos - Position) + 1;

                int Stride = Control & 0xfff;

                Position = Gpu.GetCpuAddr(Position);

                byte[] Data = AMemoryHelper.ReadBytes(Memory, Position, Size);

                GalVertexAttrib[] AttribArray = Attribs[Index]?.ToArray() ?? new GalVertexAttrib[0];

                Gpu.Renderer.SetVertexArray(Index, Stride, Data, AttribArray);
                Gpu.Renderer.RenderVertexArray(Index);
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
            if (TryGetCpuAddr(NvGpuEngine3dReg.ConstBufferNAddress, out long Position))
            {
                int Offset = ReadRegister(NvGpuEngine3dReg.ConstBufferNOffset);

                foreach (int Arg in PBEntry.Arguments)
                {
                    Memory.WriteInt32(Position + Offset, Arg);

                    Offset += 4;
                }

                WriteRegister(NvGpuEngine3dReg.ConstBufferNOffset, Offset);
            }
        }

        private void CbBind(AMemory Memory, NsGpuPBEntry PBEntry)
        {
            int Index = PBEntry.Arguments[0];

            bool Enabled = (Index & 1) != 0;

            Index = (Index >> 4) & 0x1f;

            if (TryGetCpuAddr(NvGpuEngine3dReg.ConstBufferNAddress, out long Position))
            {
                Cbs[Index].Position = Position;
                Cbs[Index].Enabled  = Enabled;
                Cbs[Index].Size     = ReadRegister(NvGpuEngine3dReg.ConstBufferNSize);
            }
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