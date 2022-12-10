using System;

namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    [Flags]
    internal enum PartitionType
    {
        PartitionNone,
        PartitionHorz,
        PartitionVert,
        PartitionSplit,
        PartitionTypes,
        PartitionInvalid = PartitionTypes
    }
}
