using ARMeilleure.CodeGen.RegisterAllocators;
using ARMeilleure.IntermediateRepresentation;
using System.Collections.Generic;
using System.IO;

namespace ARMeilleure.CodeGen.X86
{
    class CodeGenContext
    {
        private const int ReservedBytesForJump = 1;

        private Stream _stream;

        public RAReport RAReport { get; }

        public Assembler Assembler { get; }

        public BasicBlock CurrBlock { get; private set; }

        private struct Jump
        {
            public bool IsConditional { get; }

            public X86Condition Condition { get; }

            public BasicBlock Target { get; }

            public long JumpPosition { get; }

            public long RelativeOffset { get; set; }

            public int InstSize { get; set; }

            public Jump(BasicBlock target, long jumpPosition)
            {
                IsConditional = false;
                Condition     = 0;
                Target        = target;
                JumpPosition  = jumpPosition;

                RelativeOffset = 0;

                InstSize = 0;
            }

            public Jump(X86Condition condition, BasicBlock target, long jumpPosition)
            {
                IsConditional = true;
                Condition     = condition;
                Target        = target;
                JumpPosition  = jumpPosition;

                RelativeOffset = 0;

                InstSize = 0;
            }
        }

        private long[] _blockOffsets;

        private List<Jump> _jumps;

        public CodeGenContext(Stream stream, RAReport raReport, int blocksCount)
        {
            _stream = stream;

            RAReport = raReport;

            Assembler = new Assembler(stream);

            _blockOffsets = new long[blocksCount];

            _jumps = new List<Jump>();
        }

        public void EnterBlock(BasicBlock block)
        {
            _blockOffsets[block.Index] = _stream.Position;

            CurrBlock = block;
        }

        public void JumpTo(BasicBlock target)
        {
            _jumps.Add(new Jump(target, _stream.Position));

            WritePadding(ReservedBytesForJump);
        }

        public void JumpTo(X86Condition condition, BasicBlock target)
        {
            _jumps.Add(new Jump(condition, target, _stream.Position));

            WritePadding(ReservedBytesForJump);
        }

        private void WritePadding(int size)
        {
            while (size-- > 0)
            {
                _stream.WriteByte(0);
            }
        }

        public byte[] GetCode()
        {
            //Write jump relative offsets.
            bool modified;

            do
            {
                modified = false;

                for (int index = 0; index < _jumps.Count; index++)
                {
                    Jump jump = _jumps[index];

                    long jumpTarget = _blockOffsets[jump.Target.Index];

                    long offset = jumpTarget - jump.JumpPosition;

                    if (offset < 0)
                    {
                        for (int index2 = index - 1; index2 >= 0; index2--)
                        {
                            Jump jump2 = _jumps[index2];

                            if (jump2.JumpPosition < jumpTarget)
                            {
                                break;
                            }

                            offset -= jump2.InstSize - ReservedBytesForJump;
                        }
                    }
                    else
                    {
                        for (int index2 = index + 1; index2 < _jumps.Count; index2++)
                        {
                            Jump jump2 = _jumps[index2];

                            if (jump2.JumpPosition >= jumpTarget)
                            {
                                break;
                            }

                            offset += jump2.InstSize - ReservedBytesForJump;
                        }

                        offset -= ReservedBytesForJump;
                    }

                    if (jump.IsConditional)
                    {
                        jump.InstSize = Assembler.GetJccLength(offset);
                    }
                    else
                    {
                        jump.InstSize = Assembler.GetJmpLength(offset);
                    }

                    //The jump is relative to the next instruction, not the current one.
                    //Since we didn't know the next instruction address when calculating
                    //the offset (as the size of the current jump instruction was not know),
                    //we now need to compensate the offset with the jump instruction size.
                    //It's also worth to note that:
                    //- This is only needed for backward jumps.
                    //- The GetJmpLength and GetJccLength also compensates the offset
                    //internally when computing the jump instruction size.
                    if (offset < 0)
                    {
                        offset -= jump.InstSize;
                    }

                    if (jump.RelativeOffset != offset)
                    {
                        modified = true;
                    }

                    jump.RelativeOffset = offset;

                    _jumps[index] = jump;
                }
            }
            while (modified);

            //Write the code, ignoring the dummy bytes after jumps, into a new stream.
            _stream.Seek(0, SeekOrigin.Begin);

            using (MemoryStream codeStream = new MemoryStream())
            {
                Assembler assembler = new Assembler(codeStream);

                byte[] buffer;

                for (int index = 0; index < _jumps.Count; index++)
                {
                    Jump jump = _jumps[index];

                    buffer = new byte[jump.JumpPosition - _stream.Position];

                    _stream.Read(buffer, 0, buffer.Length);
                    _stream.Seek(ReservedBytesForJump, SeekOrigin.Current);

                    codeStream.Write(buffer);

                    if (jump.IsConditional)
                    {
                        assembler.Jcc(jump.Condition, jump.RelativeOffset);
                    }
                    else
                    {
                        assembler.Jmp(jump.RelativeOffset);
                    }
                }

                buffer = new byte[_stream.Length - _stream.Position];

                _stream.Read(buffer, 0, buffer.Length);

                codeStream.Write(buffer);

                return codeStream.ToArray();
            }
        }
    }
}