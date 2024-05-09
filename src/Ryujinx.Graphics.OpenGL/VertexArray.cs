using Silk.NET.OpenGL.Legacy;
using Ryujinx.Graphics.GAL;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.OpenGL
{
    class VertexArray : IDisposable
    {
        public uint Handle { get; private set; }

        private readonly VertexAttribDescriptor[] _vertexAttribs;
        private readonly VertexBufferDescriptor[] _vertexBuffers;

        private int _minVertexCount;

        private uint _vertexAttribsInUse;
        private uint _vertexBuffersInUse;
        private uint _vertexBuffersLimited;

        private BufferRange _indexBuffer;
        private readonly BufferHandle _tempIndexBuffer;
        private BufferHandle _tempVertexBuffer;
        private int _tempVertexBufferSize;
        private readonly GL _api;

        public VertexArray(GL api)
        {
            _api = api;
            Handle = _api.GenVertexArray();

            _vertexAttribs = new VertexAttribDescriptor[Constants.MaxVertexAttribs];
            _vertexBuffers = new VertexBufferDescriptor[Constants.MaxVertexBuffers];

            _tempIndexBuffer = Buffer.Create(_api);
        }

        public void Bind()
        {
            _api.BindVertexArray(Handle);
        }

        public void SetVertexBuffers(ReadOnlySpan<VertexBufferDescriptor> vertexBuffers)
        {
            int minVertexCount = int.MaxValue;

            int bindingIndex;
            for (bindingIndex = 0; bindingIndex < vertexBuffers.Length; bindingIndex++)
            {
                VertexBufferDescriptor vb = vertexBuffers[bindingIndex];

                if (vb.Buffer.Handle != BufferHandle.Null)
                {
                    int vertexCount = vb.Stride <= 0 ? 0 : vb.Buffer.Size / vb.Stride;
                    if (minVertexCount > vertexCount)
                    {
                        minVertexCount = vertexCount;
                    }

                    _api.BindVertexBuffer((uint)bindingIndex, vb.Buffer.Handle.ToUInt32(), vb.Buffer.Offset, (uint)vb.Stride);
                    _api.VertexBindingDivisor((uint)bindingIndex, (uint)vb.Divisor);
                    _vertexBuffersInUse |= 1u << bindingIndex;
                }
                else
                {
                    if ((_vertexBuffersInUse & (1u << bindingIndex)) != 0)
                    {
                        _api.BindVertexBuffer((uint)bindingIndex, 0, IntPtr.Zero, 0);
                        _vertexBuffersInUse &= ~(1u << bindingIndex);
                    }
                }

                _vertexBuffers[bindingIndex] = vb;
            }

            _minVertexCount = minVertexCount;
        }

        public void SetVertexAttributes(ReadOnlySpan<VertexAttribDescriptor> vertexAttribs)
        {
            int index = 0;

            for (; index < vertexAttribs.Length; index++)
            {
                VertexAttribDescriptor attrib = vertexAttribs[index];

                if (attrib.Equals(_vertexAttribs[index]))
                {
                    continue;
                }

                FormatInfo fmtInfo = FormatTable.GetFormatInfo(attrib.Format);

                if (attrib.IsZero)
                {
                    // Disabling the attribute causes the shader to read a constant value.
                    // We currently set the constant to (0, 0, 0, 0).
                    DisableVertexAttrib(index);
                }
                else
                {
                    EnableVertexAttrib(index);
                }

                int offset = attrib.Offset;
                int size = fmtInfo.Components;

                bool isFloat = fmtInfo.PixelType == PixelType.Float ||
                               fmtInfo.PixelType == PixelType.HalfFloat;

                if (isFloat || fmtInfo.Normalized || fmtInfo.Scaled)
                {
                    VertexAttribType type = (VertexAttribType)fmtInfo.PixelType;

                    _api.VertexAttribFormat((uint)index, size, type, fmtInfo.Normalized, (uint)offset);
                }
                else
                {
                    VertexAttribIType type = (VertexAttribIType)fmtInfo.PixelType;

                    _api.VertexAttribIFormat((uint)index, size, type, (uint)offset);
                }

                _api.VertexAttribBinding((uint)index, (uint)attrib.BufferIndex);

                _vertexAttribs[index] = attrib;
            }

            for (; index < Constants.MaxVertexAttribs; index++)
            {
                DisableVertexAttrib(index);
            }
        }

        public void SetIndexBuffer(BufferRange range)
        {
            _indexBuffer = range;
            _api.BindBuffer(BufferTargetARB.ElementArrayBuffer, range.Handle.ToUInt32());
        }

        public void SetRangeOfIndexBuffer()
        {
            Buffer.Resize(_api, _tempIndexBuffer, _indexBuffer.Size);
            Buffer.Copy(_api, _indexBuffer.Handle, _tempIndexBuffer, _indexBuffer.Offset, 0, _indexBuffer.Size);
            _api.BindBuffer(BufferTargetARB.ElementArrayBuffer, _tempIndexBuffer.ToUInt32());
        }

        public void RestoreIndexBuffer()
        {
            _api.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer.Handle.ToUInt32());
        }

        public void PreDraw(int vertexCount)
        {
            LimitVertexBuffers(vertexCount);
        }

        public void PreDrawVbUnbounded()
        {
            UnlimitVertexBuffers();
        }

        public void LimitVertexBuffers(int vertexCount)
        {
            // Is it possible for the draw to fetch outside the bounds of any vertex buffer currently bound?

            if (vertexCount <= _minVertexCount)
            {
                return;
            }

            // If the draw can fetch out of bounds, let's ensure that it will only fetch zeros rather than memory garbage.

            int currentTempVbOffset = 0;
            uint buffersInUse = _vertexBuffersInUse;

            while (buffersInUse != 0)
            {
                int vbIndex = BitOperations.TrailingZeroCount(buffersInUse);

                ref var vb = ref _vertexBuffers[vbIndex];

                int requiredSize = vertexCount * vb.Stride;

                if (vb.Buffer.Size < requiredSize)
                {
                    BufferHandle tempVertexBuffer = EnsureTempVertexBufferSize(currentTempVbOffset + requiredSize);

                    Buffer.Copy(_api, vb.Buffer.Handle, tempVertexBuffer, vb.Buffer.Offset, currentTempVbOffset, vb.Buffer.Size);
                    Buffer.Clear(_api, tempVertexBuffer, currentTempVbOffset + vb.Buffer.Size, (uint)(requiredSize - vb.Buffer.Size), 0);

                    _api.BindVertexBuffer((uint)vbIndex, tempVertexBuffer.ToUInt32(), (IntPtr)currentTempVbOffset, (uint)vb.Stride);

                    currentTempVbOffset += requiredSize;
                    _vertexBuffersLimited |= 1u << vbIndex;
                }

                buffersInUse &= ~(1u << vbIndex);
            }
        }

        private BufferHandle EnsureTempVertexBufferSize(int size)
        {
            BufferHandle tempVertexBuffer = _tempVertexBuffer;

            if (_tempVertexBufferSize < size)
            {
                _tempVertexBufferSize = size;

                if (tempVertexBuffer == BufferHandle.Null)
                {
                    tempVertexBuffer = Buffer.Create(_api, size);
                    _tempVertexBuffer = tempVertexBuffer;
                    return tempVertexBuffer;
                }

                Buffer.Resize(_api, _tempVertexBuffer, size);
            }

            return tempVertexBuffer;
        }

        public void UnlimitVertexBuffers()
        {
            uint buffersLimited = _vertexBuffersLimited;

            if (buffersLimited == 0)
            {
                return;
            }

            while (buffersLimited != 0)
            {
                int vbIndex = BitOperations.TrailingZeroCount(buffersLimited);

                ref var vb = ref _vertexBuffers[vbIndex];

                _api.BindVertexBuffer((uint)vbIndex, vb.Buffer.Handle.ToUInt32(), (IntPtr)vb.Buffer.Offset, (uint)vb.Stride);

                buffersLimited &= ~(1u << vbIndex);
            }

            _vertexBuffersLimited = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnableVertexAttrib(int index)
        {
            uint mask = 1u << index;

            if ((_vertexAttribsInUse & mask) == 0)
            {
                _vertexAttribsInUse |= mask;
                _api.EnableVertexAttribArray((uint)index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DisableVertexAttrib(int index)
        {
            uint mask = 1u << index;

            if ((_vertexAttribsInUse & mask) != 0)
            {
                _vertexAttribsInUse &= ~mask;
                _api.DisableVertexAttribArray((uint)index);
                _api.VertexAttrib4((uint)index, 0f, 0f, 0f, 1f);
            }
        }

        public void Dispose()
        {
            if (Handle != 0)
            {
                _api.DeleteVertexArray(Handle);

                Handle = 0;
            }
        }
    }
}
