using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Nvdec.Types.Vp8
{
    struct PictureInfo
    {
        public Array10<uint> Unknown0;
        public uint BitstreamSize;
        public uint BlockLayout; // Not supported on T210
        public uint WorkBufferSizeShr8;

        public uint gptimer_timeout_value;

        public ushort FrameWidth;     // actual frame width
        public ushort FrameHeight;    // actual frame height

        public byte keyFrame;        // 1: key frame; 0: not
        public byte version;
        public byte tileFormat; //                : 2 ;   // 0: TBL; 1: KBL;
        public byte gob_height;//                 : 3 ;   // Set GOB height, 0: GOB_2, 1: GOB_4, 2: GOB_8, 3: GOB_16, 4: GOB_32 (NVDEC3 onwards)
        public byte reserverd_surface_format; //   : 3 ;
        public byte errorConcealOn;  // 1: error conceal on; 0: off

        public uint firstPartSize;   // the size of first partition(frame header and mb header partition)

        // ctx
        public uint HistBufferSize;                  // in units of 256
        public uint VLDBufferSize;                   // in units of 1
                                                     // current frame buffers
        public Array2<uint> FrameStride;                  // [y_c]
        public uint luma_top_offset;                 // offset of luma top field in units of 256
        public uint luma_bot_offset;                 // offset of luma bottom field in units of 256
        public uint luma_frame_offset;               // offset of luma frame in units of 256
        public uint chroma_top_offset;               // offset of chroma top field in units of 256
        public uint chroma_bot_offset;               // offset of chroma bottom field in units of 256
        public uint chroma_frame_offset;             // offset of chroma frame in units of 256

        public uint enableTFOutput;//    : 1; //=1, enable dbfdma to output the display surface; if disable, then the following configure on tf is useless.
                                   //remap for VC1
        public uint VC1MapYFlag;//       : 1;
        public uint MapYValue;//         : 3;
        public uint VC1MapUVFlag;//      : 1;
        public uint MapUVValue;//        : 3;
        //tf
        public uint OutStride;//         : 8;
        public uint TilingFormat;//      : 3;
        public uint OutputStructure;//   : 1; //(0=frame, 1=field)
        public uint reserved0;//         :11;
        public Array2<uint> OutputTop;                   // in units of 256
        public Array2<uint> OutputBottom;                // in units of 256
                                                    //histogram
        public uint enableHistogram;//   : 1; // enable histogram info collection.
        public uint HistogramStartX;//  :12; // start X of Histogram window
        public uint HistogramStartY;//   :12; // start Y of Histogram window
        public uint reserved1;//         : 7;
        public uint HistogramEndX;//     :12; // end X of Histogram window
        public uint HistogramEndY;//     :12; // end y of Histogram window
        public uint reserved2;//         : 8;

        // decode picture buffere related
        public sbyte current_output_memory_layout;
        public Array3<sbyte> output_memory_layout;  // output NV12/NV24 setting. item 0:golden; 1: altref; 2: last

        public byte segmentation_feature_data_update;
        public Array3<byte> reserved3;

        // ucode return result
        public uint resultValue;
        public Array8<uint> partition_offset;

        public Array3<uint> reserved4;

        /*
        public uint BitstreamSize;
        public uint IsEncrypted;
        public uint Unknown38;
        public uint Reserved3C;
        public uint BlockLayout; // Not supported on T210
        public uint WorkBufferSizeShr8;
        public FrameSize LastFrameSize;
        public FrameSize GoldenFrameSize;
        public FrameSize AltFrameSize;
        public FrameSize CurrentFrameSize;
        public FrameFlags Flags;
        public Array4<sbyte> RefFrameSignBias;
        public byte FirstLevel;
        public byte SharpnessLevel;
        public byte BaseQIndex;
        public byte YDcDeltaQ;
        public byte UvAcDeltaQ;
        public byte UvDcDeltaQ;
        public byte Lossless;
        public byte TxMode;
        public byte AllowHighPrecisionMv;
        public byte InterpFilter;
        public byte ReferenceMode;
        public sbyte CompFixedRef;
        public Array2<sbyte> CompVarRef;
        public byte Log2TileCols;
        public byte Log2TileRows;
        public Segmentation Seg;
        public LoopFilter Lf;
        public byte PaddingEB;
        public uint WorkBufferSizeShr8New; // Not supported on T210
        public uint SurfaceParams; // Not supported on T210
        public uint UnknownF4;
        public uint UnknownF8;
        public uint UnknownFC;
#pragma warning restore CS0649
        */
    }
}
