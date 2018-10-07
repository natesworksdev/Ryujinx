using Ryujinx.Graphics.Memory;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.VideoDecoding
{
    unsafe class VideoDecoder
    {
        private NvGpu Gpu;

        private H264Decoder H264Decoder;
        private Vp9Decoder  Vp9Decoder;

        private VideoCodec CurrentVideoCodec;

        private long DecoderContextAddress;
        private long FrameDataAddress;
        private long VpxCurrLumaAddress;
        private long VpxRef0LumaAddress;
        private long VpxRef1LumaAddress;
        private long VpxRef2LumaAddress;
        private long VpxCurrChromaAddress;
        private long VpxRef0ChromaAddress;
        private long VpxRef1ChromaAddress;
        private long VpxRef2ChromaAddress;
        private long VpxProbTablesAddress;

        public VideoDecoder(NvGpu Gpu)
        {
            this.Gpu = Gpu;

            H264Decoder = new H264Decoder();
            Vp9Decoder  = new Vp9Decoder();
        }

        public void Process(NvGpuVmm Vmm, int MethodOffset, int[] Arguments)
        {
            VideoDecoderMeth Method = (VideoDecoderMeth)MethodOffset;

            switch (Method)
            {
                case VideoDecoderMeth.SetVideoCodec:        SetVideoCodec       (Vmm, Arguments); break;
                case VideoDecoderMeth.Execute:              Execute             (Vmm, Arguments); break;
                case VideoDecoderMeth.SetDecoderCtxAddr:    SetDecoderCtxAddr   (Vmm, Arguments); break;
                case VideoDecoderMeth.SetFrameDataAddr:     SetFrameDataAddr    (Vmm, Arguments); break;
                case VideoDecoderMeth.SetVpxCurrLumaAddr:   SetVpxCurrLumaAddr  (Vmm, Arguments); break;
                case VideoDecoderMeth.SetVpxRef0LumaAddr:   SetVpxRef0LumaAddr  (Vmm, Arguments); break;
                case VideoDecoderMeth.SetVpxRef1LumaAddr:   SetVpxRef1LumaAddr  (Vmm, Arguments); break;
                case VideoDecoderMeth.SetVpxRef2LumaAddr:   SetVpxRef2LumaAddr  (Vmm, Arguments); break;
                case VideoDecoderMeth.SetVpxCurrChromaAddr: SetVpxCurrChromaAddr(Vmm, Arguments); break;
                case VideoDecoderMeth.SetVpxRef0ChromaAddr: SetVpxRef0ChromaAddr(Vmm, Arguments); break;
                case VideoDecoderMeth.SetVpxRef1ChromaAddr: SetVpxRef1ChromaAddr(Vmm, Arguments); break;
                case VideoDecoderMeth.SetVpxRef2ChromaAddr: SetVpxRef2ChromaAddr(Vmm, Arguments); break;
                case VideoDecoderMeth.SetVpxProbTablesAddr: SetVpxProbTablesAddr(Vmm, Arguments); break;
            }
        }

        private void SetVideoCodec(NvGpuVmm Vmm, int[] Arguments)
        {
            CurrentVideoCodec = (VideoCodec)Arguments[0];
        }

        private void Execute(NvGpuVmm Vmm, int[] Arguments)
        {
            if (CurrentVideoCodec == VideoCodec.H264)
            {
                int FrameDataSize = Vmm.ReadInt32(DecoderContextAddress + 0x48);

                IntPtr HeaderPtr = Vmm.GetHostAddress(DecoderContextAddress + 0x58, Marshal.SizeOf<H264ParameterSets>());

                H264ParameterSets Params = Marshal.PtrToStructure<H264ParameterSets>(HeaderPtr);

                H264Matrices Matrices = new H264Matrices()
                {
                    ScalingMatrix4 = Vmm.ReadBytes(DecoderContextAddress + 0x1c0, 6 * 16),
                    ScalingMatrix8 = Vmm.ReadBytes(DecoderContextAddress + 0x220, 2 * 64)
                };

                byte[] FrameData = Vmm.ReadBytes(FrameDataAddress, FrameDataSize);

                H264Decoder.Decode(Params, Matrices, FrameData);
            }
            else if (CurrentVideoCodec == VideoCodec.Vp9)
            {
                int FrameDataSize = Vmm.ReadInt32(DecoderContextAddress + 0x30);

                IntPtr HeaderPtr = Vmm.GetHostAddress(DecoderContextAddress + 0x48, Marshal.SizeOf<Vp9FrameHeader>());

                Vp9FrameKeys Keys = new Vp9FrameKeys()
                {
                    CurrKey = Vmm.GetPhysicalAddress(VpxCurrLumaAddress),
                    Ref0Key = Vmm.GetPhysicalAddress(VpxRef0LumaAddress),
                    Ref1Key = Vmm.GetPhysicalAddress(VpxRef1LumaAddress),
                    Ref2Key = Vmm.GetPhysicalAddress(VpxRef2LumaAddress)
                };

                Vp9FrameHeader Header = Marshal.PtrToStructure<Vp9FrameHeader>(HeaderPtr);

                Vp9ProbabilityTables Probs = new Vp9ProbabilityTables()
                {
                    SegmentationTreeProbs = Vmm.ReadBytes(VpxProbTablesAddress + 0x387, 0x7),
                    SegmentationPredProbs = Vmm.ReadBytes(VpxProbTablesAddress + 0x38e, 0x3),
                    Tx8x8Probs            = Vmm.ReadBytes(VpxProbTablesAddress + 0x470, 0x2),
                    Tx16x16Probs          = Vmm.ReadBytes(VpxProbTablesAddress + 0x472, 0x4),
                    Tx32x32Probs          = Vmm.ReadBytes(VpxProbTablesAddress + 0x476, 0x6),
                    CoefProbs             = Vmm.ReadBytes(VpxProbTablesAddress + 0x5a0, 0x900),
                    SkipProbs             = Vmm.ReadBytes(VpxProbTablesAddress + 0x537, 0x3),
                    InterModeProbs        = Vmm.ReadBytes(VpxProbTablesAddress + 0x400, 0x28),
                    InterpFilterProbs     = Vmm.ReadBytes(VpxProbTablesAddress + 0x52a, 0x8),
                    IsInterProbs          = Vmm.ReadBytes(VpxProbTablesAddress + 0x41c, 0x4),
                    CompModeProbs         = Vmm.ReadBytes(VpxProbTablesAddress + 0x532, 0x5),
                    SingleRefProbs        = Vmm.ReadBytes(VpxProbTablesAddress + 0x580, 0xa),
                    CompRefProbs          = Vmm.ReadBytes(VpxProbTablesAddress + 0x58a, 0x5),
                    YModeProbs0           = Vmm.ReadBytes(VpxProbTablesAddress + 0x480, 0x20),
                    YModeProbs1           = Vmm.ReadBytes(VpxProbTablesAddress + 0x47c, 0x4),
                    PartitionProbs        = Vmm.ReadBytes(VpxProbTablesAddress + 0x4e0, 0x40),
                    MvJointProbs          = Vmm.ReadBytes(VpxProbTablesAddress + 0x53b, 0x3),
                    MvSignProbs           = Vmm.ReadBytes(VpxProbTablesAddress + 0x53e, 0x3),
                    MvClassProbs          = Vmm.ReadBytes(VpxProbTablesAddress + 0x54c, 0x14),
                    MvClass0BitProbs      = Vmm.ReadBytes(VpxProbTablesAddress + 0x540, 0x3),
                    MvBitsProbs           = Vmm.ReadBytes(VpxProbTablesAddress + 0x56c, 0x14),
                    MvClass0FrProbs       = Vmm.ReadBytes(VpxProbTablesAddress + 0x560, 0xc),
                    MvFrProbs             = Vmm.ReadBytes(VpxProbTablesAddress + 0x542, 0x6),
                    MvClass0HpProbs       = Vmm.ReadBytes(VpxProbTablesAddress + 0x548, 0x2),
                    MvHpProbs             = Vmm.ReadBytes(VpxProbTablesAddress + 0x54a, 0x2)
                };

                byte[] FrameData = Vmm.ReadBytes(FrameDataAddress, FrameDataSize);

                Vp9Decoder.Decode(Keys, Header, Probs, FrameData);
            }
            else
            {
                ThrowUnimplementedCodec();
            }
        }

        private void SetDecoderCtxAddr(NvGpuVmm Vmm, int[] Arguments)
        {
            DecoderContextAddress = GetAddress(Arguments);
        }

        private void SetFrameDataAddr(NvGpuVmm Vmm, int[] Arguments)
        {
            FrameDataAddress = GetAddress(Arguments);
        }

        private void SetVpxCurrLumaAddr(NvGpuVmm Vmm, int[] Arguments)
        {
            VpxCurrLumaAddress = GetAddress(Arguments);
        }

        private void SetVpxRef0LumaAddr(NvGpuVmm Vmm, int[] Arguments)
        {
            VpxRef0LumaAddress = GetAddress(Arguments);
        }

        private void SetVpxRef1LumaAddr(NvGpuVmm Vmm, int[] Arguments)
        {
            VpxRef1LumaAddress = GetAddress(Arguments);
        }

        private void SetVpxRef2LumaAddr(NvGpuVmm Vmm, int[] Arguments)
        {
            VpxRef2LumaAddress = GetAddress(Arguments);
        }

        private void SetVpxCurrChromaAddr(NvGpuVmm Vmm, int[] Arguments)
        {
            VpxCurrChromaAddress = GetAddress(Arguments);
        }

        private void SetVpxRef0ChromaAddr(NvGpuVmm Vmm, int[] Arguments)
        {
            VpxRef0ChromaAddress = GetAddress(Arguments);
        }

        private void SetVpxRef1ChromaAddr(NvGpuVmm Vmm, int[] Arguments)
        {
            VpxRef1ChromaAddress = GetAddress(Arguments);
        }

        private void SetVpxRef2ChromaAddr(NvGpuVmm Vmm, int[] Arguments)
        {
            VpxRef2ChromaAddress = GetAddress(Arguments);
        }

        private void SetVpxProbTablesAddr(NvGpuVmm Vmm, int[] Arguments)
        {
            VpxProbTablesAddress = GetAddress(Arguments);
        }

        private static long GetAddress(int[] Arguments)
        {
            return (long)(uint)Arguments[0] << 8;
        }

        internal void CopyPlanes(NvGpuVmm Vmm, long LumaPlaneAddress, long ChromaPlaneAddress)
        {
            FFmpegFrame Frame = FFmpegWrapper.GetFrame();

            int HalfWidth  = Frame.Width  / 2;
            int HalfHeight = Frame.Height / 2;

            int AlignedWidth = (Frame.Width + 0xff) & ~0xff;

            byte* LumaPtr   = (byte*)Vmm.GetHostAddress(LumaPlaneAddress,   AlignedWidth * Frame.Height);
            byte* ChromaPtr = (byte*)Vmm.GetHostAddress(ChromaPlaneAddress, AlignedWidth * HalfHeight);

            for (int Y = 0; Y < Frame.Height; Y++)
            {
                int Src = Y * Frame.Width;
                int Dst = Y * AlignedWidth;

                Buffer.MemoryCopy(Frame.LumaPtr + Src, LumaPtr + Dst, Frame.Width, Frame.Width);
            }

            //Copy chroma data from both channels with interleaving.
            for (int Y = 0; Y < HalfHeight; Y++)
            {
                int Src = Y * HalfWidth;
                int Dst = Y * AlignedWidth;

                for (int X = 0; X < HalfWidth; X++)
                {
                    *(ChromaPtr + Dst + X * 2 + 0) = *(Frame.ChromaBPtr + Src + X);
                    *(ChromaPtr + Dst + X * 2 + 1) = *(Frame.ChromaRPtr + Src + X);
                }
            }
        }

        private void ThrowUnimplementedCodec()
        {
            throw new NotImplementedException("Codec \"" + CurrentVideoCodec + "\" is not supported!");
        }
    }
}