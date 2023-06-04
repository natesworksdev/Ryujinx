using Ryujinx.Common;
using System;

namespace Ryujinx.Audio.Backends.Common
{
    /// <summary>
    /// A ring buffer that grow if data written to it is too big to fit.
    /// </summary>
    public class DynamicRingBuffer
    {
        private const int RingBufferAlignment = 2048;

        private readonly object _lock = new();

        private byte[] _buffer;
        private int _headOffset;
        private int _tailOffset;

        public int Length { get; private set; }

        public DynamicRingBuffer(int initialCapacity = RingBufferAlignment)
        {
            _buffer = new byte[initialCapacity];
        }

        public void Clear()
        {
            Length = 0;
            _headOffset = 0;
            _tailOffset = 0;
        }

        public void Clear(int size)
        {
            lock (_lock)
            {
                if (size > Length)
                {
                    size = Length;
                }

                if (size == 0)
                {
                    return;
                }

                _headOffset = (_headOffset + size) % _buffer.Length;
                Length -= size;

                if (Length == 0)
                {
                    _headOffset = 0;
                    _tailOffset = 0;
                }
            }
        }

        private void SetCapacityLocked(int capacity)
        {
            byte[] buffer = new byte[capacity];

            if (Length > 0)
            {
                if (_headOffset < _tailOffset)
                {
                    Buffer.BlockCopy(_buffer, _headOffset, buffer, 0, Length);
                }
                else
                {
                    Buffer.BlockCopy(_buffer, _headOffset, buffer, 0, _buffer.Length - _headOffset);
                    Buffer.BlockCopy(_buffer, 0, buffer, _buffer.Length - _headOffset, _tailOffset);
                }
            }

            _buffer = buffer;
            _headOffset = 0;
            _tailOffset = Length;
        }


        public void Write<T>(T[] buffer, int index, int count)
        {
            if (count == 0)
            {
                return;
            }

            lock (_lock)
            {
                if ((Length + count) > _buffer.Length)
                {
                    SetCapacityLocked(BitUtils.AlignUp(Length + count, RingBufferAlignment));
                }

                if (_headOffset < _tailOffset)
                {
                    int tailLength = _buffer.Length - _tailOffset;

                    if (tailLength >= count)
                    {
                        Buffer.BlockCopy(buffer, index, _buffer, _tailOffset, count);
                    }
                    else
                    {
                        Buffer.BlockCopy(buffer, index, _buffer, _tailOffset, tailLength);
                        Buffer.BlockCopy(buffer, index + tailLength, _buffer, 0, count - tailLength);
                    }
                }
                else
                {
                    Buffer.BlockCopy(buffer, index, _buffer, _tailOffset, count);
                }

                Length += count;
                _tailOffset = (_tailOffset + count) % _buffer.Length;
            }
        }

        public int Read<T>(T[] buffer, int index, int count)
        {
            lock (_lock)
            {
                if (count > Length)
                {
                    count = Length;
                }

                if (count == 0)
                {
                    return 0;
                }

                if (_headOffset < _tailOffset)
                {
                    Buffer.BlockCopy(_buffer, _headOffset, buffer, index, count);
                }
                else
                {
                    int tailLength = _buffer.Length - _headOffset;

                    if (tailLength >= count)
                    {
                        Buffer.BlockCopy(_buffer, _headOffset, buffer, index, count);
                    }
                    else
                    {
                        Buffer.BlockCopy(_buffer, _headOffset, buffer, index, tailLength);
                        Buffer.BlockCopy(_buffer, 0, buffer, index + tailLength, count - tailLength);
                    }
                }

                Length -= count;
                _headOffset = (_headOffset + count) % _buffer.Length;

                if (Length == 0)
                {
                    _headOffset = 0;
                    _tailOffset = 0;
                }

                return count;
            }
        }
    }
}