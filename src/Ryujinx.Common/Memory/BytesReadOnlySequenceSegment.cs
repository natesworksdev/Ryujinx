using System;
using System.Buffers;

namespace Ryujinx.Common.Memory
{
    /// <summary>
    /// A concrete implementation of <seealso cref="ReadOnlySequence{Byte}"/>,
    /// with methods to help build a full sequence.
    /// </summary>
    public sealed class BytesReadOnlySequenceSegment : ReadOnlySequenceSegment<byte>
    {
        public BytesReadOnlySequenceSegment(Memory<byte> memory) => Memory = memory;

        public BytesReadOnlySequenceSegment Append(Memory<byte> memory)
        {
            var nextSegment = new BytesReadOnlySequenceSegment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };

            Next = nextSegment;

            return nextSegment;
        }
    }
}
