using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Common;
using Ryujinx.Graphics.Video;

namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal struct Vp9Common
    {
        public MacroBlockD Mb;

        public ArrayPtr<TileWorkerData> TileWorkerData;

        public void InitializeTileWorkerData(int tileCols, int tileRows)
        {
            TileWorkerData = MemoryUtil.Allocate<TileWorkerData>(tileCols * tileRows);
        }

        public InternalErrorInfo Error;

        public int Width;
        public int Height;

        public int SubsamplingX;
        public int SubsamplingY;

        public ArrayPtr<MvRef> PrevFrameMvs;
        public ArrayPtr<MvRef> CurFrameMvs;

        public Array3<RefBuffer> FrameRefs;

        public int NewFbIdx;

        public FrameType FrameType;

        // Flag signaling that the frame is encoded using only Intra modes.
        public bool IntraOnly;

        public bool AllowHighPrecisionMv;

        // MBs, MbRows/Cols is in 16-pixel units; MiRows/Cols is in
        // ModeInfo (8-pixel) units.
        public int MBs;
        public int MbRows, MiRows;
        public int MbCols, MiCols;
        public int MiStride;

        /* Profile settings */
        public TxMode TxMode;

        public int BaseQindex;
        public int YDcDeltaQ;
        public int UvDcDeltaQ;
        public int UvAcDeltaQ;
        public Array8<Array2<short>> YDequant;
        public Array8<Array2<short>> UvDequant;

        /* We allocate a ModeInfo struct for each macroblock, together with
           an extra row on top and column on the left to simplify prediction. */
        public int MiAllocSize;
        public ArrayPtr<ModeInfo> Mip; /* Base of allocated array */
        public ArrayPtr<ModeInfo> Mi;  /* Corresponds to upper left visible macroblock */

        public ArrayPtr<Ptr<ModeInfo>> MiGridBase;
        public ArrayPtr<Ptr<ModeInfo>> MiGridVisible;

        // Whether to use previous frame's motion vectors for prediction.
        public bool UsePrevFrameMvs;

        // Persistent mb segment id map used in prediction.
        public int SegMapIdx;
        public int PrevSegMapIdx;

        public Array2<ArrayPtr<byte>> SegMapArray;
        public ArrayPtr<byte> LastFrameSegMap;
        public ArrayPtr<byte> CurrentFrameSegMap;
        public int SegMapAllocSize;

        public byte InterpFilter;

        public LoopFilterInfoN LfInfo;

        public Array4<sbyte> RefFrameSignBias; /* Two state 0, 1 */

        public LoopFilter Lf;
        public Segmentation Seg;

        // Context probabilities for reference frame prediction
        public sbyte CompFixedRef;
        public Array2<sbyte> CompVarRef;
        public ReferenceMode ReferenceMode;

        public Ptr<Vp9EntropyProbs> Fc;
        public Ptr<Vp9BackwardUpdates> Counts;

        public int FrameParallelDecodingMode;

        public int Log2TileCols, Log2TileRows;

        public ArrayPtr<sbyte> AboveSegContext;
        public ArrayPtr<sbyte> AboveContext;
        public int AboveContextAllocCols;

        public bool FrameIsIntraOnly()
        {
            return FrameType == FrameType.KeyFrame || IntraOnly;
        }

        public bool CompoundReferenceAllowed()
        {
            int i;
            for (i = 1; i < Constants.RefsPerFrame; ++i)
            {
                if (RefFrameSignBias[i + 1] != RefFrameSignBias[1])
                {
                    return true;
                }
            }

            return false;
        }

        private static int CalcMiSize(int len)
        {
            // Len is in mi units.
            return len + Constants.MiBlockSize;
        }

        public void SetMbMi(int width, int height)
        {
            int alignedWidth = BitUtils.AlignPowerOfTwo(width, Constants.MiSizeLog2);
            int alignedHeight = BitUtils.AlignPowerOfTwo(height, Constants.MiSizeLog2);

            MiCols = alignedWidth >> Constants.MiSizeLog2;
            MiRows = alignedHeight >> Constants.MiSizeLog2;
            MiStride = CalcMiSize(MiCols);

            MbCols = (MiCols + 1) >> 1;
            MbRows = (MiRows + 1) >> 1;
            MBs = MbRows * MbCols;
        }

        private bool AllocSegMap(int segMapSize)
        {
            int i;

            for (i = 0; i < Constants.NumPingPongBuffers; ++i)
            {
                SegMapArray[i] = MemoryUtil.Allocate<byte>(segMapSize);
                if (SegMapArray[i].IsNull)
                {
                    return true;
                }
            }
            SegMapAllocSize = segMapSize;

            // Init the index.
            SegMapIdx = 0;
            PrevSegMapIdx = 1;

            CurrentFrameSegMap = SegMapArray[SegMapIdx];
            LastFrameSegMap = SegMapArray[PrevSegMapIdx];

            return false;
        }

        private void FreeSegMap()
        {
            int i;

            for (i = 0; i < Constants.NumPingPongBuffers; ++i)
            {
                MemoryUtil.Free(SegMapArray[i]);
                SegMapArray[i] = ArrayPtr<byte>.Null;
            }

            CurrentFrameSegMap = ArrayPtr<byte>.Null;
            LastFrameSegMap = ArrayPtr<byte>.Null;
        }

        private bool DecAllocMi(int miSize)
        {
            Mip = MemoryUtil.Allocate<ModeInfo>(miSize);
            if (Mip.IsNull)
            {
                return true;
            }

            MiAllocSize = miSize;
            MiGridBase = MemoryUtil.Allocate<Ptr<ModeInfo>>(miSize);
            if (MiGridBase.IsNull)
            {
                return true;
            }

            return false;
        }

        private void DecFreeMi()
        {
            MemoryUtil.Free(Mip);
            Mip = ArrayPtr<ModeInfo>.Null;
            MemoryUtil.Free(MiGridBase);
            MiGridBase = ArrayPtr<Ptr<ModeInfo>>.Null;
            MiAllocSize = 0;
        }

        public void FreeContextBuffers()
        {
            DecFreeMi();
            FreeSegMap();
            MemoryUtil.Free(AboveContext);
            AboveContext = ArrayPtr<sbyte>.Null;
            MemoryUtil.Free(AboveSegContext);
            AboveSegContext = ArrayPtr<sbyte>.Null;
            MemoryUtil.Free(Lf.Lfm);
            Lf.Lfm = ArrayPtr<LoopFilterMask>.Null;
            MemoryUtil.Free(PrevFrameMvs);
            PrevFrameMvs = ArrayPtr<MvRef>.Null;
            MemoryUtil.Free(CurFrameMvs);
            CurFrameMvs = ArrayPtr<MvRef>.Null;
        }

        private bool AllocLoopFilter()
        {
            MemoryUtil.Free(Lf.Lfm);
            // Each lfm holds bit masks for all the 8x8 blocks in a 64x64 region. The
            // stride and rows are rounded up / truncated to a multiple of 8.
            Lf.LfmStride = (MiCols + (Constants.MiBlockSize - 1)) >> 3;
            Lf.Lfm = MemoryUtil.Allocate<LoopFilterMask>(((MiRows + (Constants.MiBlockSize - 1)) >> 3) * Lf.LfmStride);
            if (Lf.Lfm.IsNull)
            {
                return true;
            }

            return false;
        }

        public bool AllocContextBuffers(int width, int height)
        {
            int newMiSize;

            SetMbMi(width, height);
            newMiSize = MiStride * CalcMiSize(MiRows);
            if (MiAllocSize < newMiSize)
            {
                DecFreeMi();
                if (DecAllocMi(newMiSize))
                {
                    goto Fail;
                }
            }

            if (SegMapAllocSize < MiRows * MiCols)
            {
                // Create the segmentation map structure and set to 0.
                FreeSegMap();
                if (AllocSegMap(MiRows * MiCols))
                {
                    goto Fail;
                }
            }

            if (AboveContextAllocCols < MiCols)
            {
                MemoryUtil.Free(AboveContext);
                AboveContext = MemoryUtil.Allocate<sbyte>(2 * TileInfo.MiColsAlignedToSb(MiCols) * Constants.MaxMbPlane);
                if (AboveContext.IsNull)
                {
                    goto Fail;
                }

                MemoryUtil.Free(AboveSegContext);
                AboveSegContext = MemoryUtil.Allocate<sbyte>(TileInfo.MiColsAlignedToSb(MiCols));
                if (AboveSegContext.IsNull)
                {
                    goto Fail;
                }

                AboveContextAllocCols = MiCols;
            }

            if (AllocLoopFilter())
            {
                goto Fail;
            }

            PrevFrameMvs = MemoryUtil.Allocate<MvRef>(MiRows * MiCols);
            CurFrameMvs = MemoryUtil.Allocate<MvRef>(MiRows * MiCols);

            return false;

        Fail:
            // clear the mi_* values to force a realloc on resync
            SetMbMi(0, 0);
            FreeContextBuffers();
            return true;
        }

        private unsafe void DecSetupMi()
        {
            Mi = Mip.Slice(MiStride + 1);
            MiGridVisible = MiGridBase.Slice(MiStride + 1);
            MemoryUtil.Fill(MiGridBase.ToPointer(), Ptr<ModeInfo>.Null, MiStride * (MiRows + 1));
        }

        public unsafe void InitContextBuffers()
        {
            DecSetupMi();
            if (!LastFrameSegMap.IsNull)
            {
                MemoryUtil.Fill(LastFrameSegMap.ToPointer(), (byte)0, MiRows * MiCols);
            }
        }

        private void SetPartitionProbs(ref MacroBlockD xd)
        {
            xd.PartitionProbs = FrameIsIntraOnly()
                ? new ArrayPtr<Array3<byte>>(ref Fc.Value.KfPartitionProb[0], 16)
                : new ArrayPtr<Array3<byte>>(ref Fc.Value.PartitionProb[0], 16);
        }

        internal void InitMacroBlockD(ref MacroBlockD xd, ArrayPtr<int> dqcoeff)
        {
            int i;

            for (i = 0; i < Constants.MaxMbPlane; ++i)
            {
                xd.Plane[i].DqCoeff = dqcoeff;
                xd.AboveContext[i] = AboveContext.Slice(i * 2 * TileInfo.MiColsAlignedToSb(MiCols));

                if (i == 0)
                {
                    MemoryUtil.Copy(ref xd.Plane[i].SegDequant, ref YDequant);
                }
                else
                {
                    MemoryUtil.Copy(ref xd.Plane[i].SegDequant, ref UvDequant);
                }
                xd.Fc = new Ptr<Vp9EntropyProbs>(ref Fc.Value);
            }

            xd.AboveSegContext = AboveSegContext;
            xd.MiStride = MiStride;
            xd.ErrorInfo = new Ptr<InternalErrorInfo>(ref Error);

            SetPartitionProbs(ref xd);
        }

        public void SetupSegmentationDequant()
        {
            const BitDepth bitDepth = BitDepth.Bits8; // TODO: Configurable
            // Build y/uv dequant values based on segmentation.
            if (Seg.Enabled)
            {
                int i;
                for (i = 0; i < Constants.MaxSegments; ++i)
                {
                    int qIndex = QuantCommon.GetQIndex(ref Seg, i, BaseQindex);
                    YDequant[i][0] = QuantCommon.DcQuant(qIndex, YDcDeltaQ, bitDepth);
                    YDequant[i][1] = QuantCommon.AcQuant(qIndex, 0, bitDepth);
                    UvDequant[i][0] = QuantCommon.DcQuant(qIndex, UvDcDeltaQ, bitDepth);
                    UvDequant[i][1] = QuantCommon.AcQuant(qIndex, UvAcDeltaQ, bitDepth);
                }
            }
            else
            {
                int qIndex = BaseQindex;
                // When segmentation is disabled, only the first value is used.  The
                // remaining are don't cares.
                YDequant[0][0] = QuantCommon.DcQuant(qIndex, YDcDeltaQ, bitDepth);
                YDequant[0][1] = QuantCommon.AcQuant(qIndex, 0, bitDepth);
                UvDequant[0][0] = QuantCommon.DcQuant(qIndex, UvDcDeltaQ, bitDepth);
                UvDequant[0][1] = QuantCommon.AcQuant(qIndex, UvAcDeltaQ, bitDepth);
            }
        }

        public void SetupScaleFactors()
        {
            for (int i = 0; i < Constants.RefsPerFrame; ++i)
            {
                ref RefBuffer refBuf = ref FrameRefs[i];
                refBuf.Sf.SetupScaleFactorsForFrame(refBuf.Buf.Width, refBuf.Buf.Height, Width, Height);
            }
        }
    }
}
