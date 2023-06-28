using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Gpu.Memory
{
    class SupportBufferUpdater : IDisposable
    {
        private SupportBuffer _data;
        private BufferHandle _handle;

        private readonly IRenderer _renderer;
        private int _startOffset = -1;
        private int _endOffset = -1;

        private readonly Vector4<float>[] _renderScale = new Vector4<float>[73];
        private readonly Vector4<int>[] _fpIsBgra = new Vector4<int>[SupportBuffer.FragmentIsBgraCount];
        private int _fragmentScaleCount;

        public SupportBufferUpdater(IRenderer renderer)
        {
            _renderer = renderer;

            var defaultScale = new Vector4<float> { X = 1f, Y = 0f, Z = 0f, W = 0f };
            new Span<Vector4<float>>(_renderScale).Fill(defaultScale);
        }

        private void MarkDirty(int startOffset, int byteSize)
        {
            int endOffset = startOffset + byteSize;

            if (_startOffset == -1)
            {
                _startOffset = startOffset;
                _endOffset = endOffset;
            }
            else
            {
                if (startOffset < _startOffset)
                {
                    _startOffset = startOffset;
                }

                if (endOffset > _endOffset)
                {
                    _endOffset = endOffset;
                }
            }
        }

        private void UpdateFragmentRenderScaleCount(int count)
        {
            if (_data.FragmentRenderScaleCount.X != count)
            {
                _data.FragmentRenderScaleCount.X = count;

                MarkDirty(SupportBuffer.FragmentRenderScaleCountOffset, sizeof(int));
            }
        }

        private void UpdateGenericField<T>(int baseOffset, ReadOnlySpan<T> data, Span<T> target, int offset, int count) where T : unmanaged
        {
            data.Slice(0, count).CopyTo(target.Slice(offset));

            int elemSize = Unsafe.SizeOf<T>();

            MarkDirty(baseOffset + offset * elemSize, count * elemSize);
        }

        private void UpdateRenderScale(ReadOnlySpan<Vector4<float>> data, int offset, int count)
        {
            UpdateGenericField(SupportBuffer.GraphicsRenderScaleOffset, data, _data.RenderScale.AsSpan(), offset, count);
        }

        private void UpdateFragmentIsBgra(ReadOnlySpan<Vector4<int>> data, int offset, int count)
        {
            UpdateGenericField(SupportBuffer.FragmentIsBgraOffset, data, _data.FragmentIsBgra.AsSpan(), offset, count);
        }

        private void UpdateViewportInverse(Vector4<float> data)
        {
            _data.ViewportInverse = data;

            MarkDirty(SupportBuffer.ViewportInverseOffset, SupportBuffer.FieldSize);
        }

        public void SetRenderTargetScale(float scale)
        {
            _renderScale[0].X = scale;
            UpdateRenderScale(_renderScale, 0, 1); // Just the first element.
        }

        public void UpdateRenderScale(ReadOnlySpan<float> scales, int totalCount, int fragmentCount)
        {
            bool changed = false;

            for (int index = 0; index < totalCount; index++)
            {
                if (_renderScale[1 + index].X != scales[index])
                {
                    _renderScale[1 + index].X = scales[index];
                    changed = true;
                }
            }

            // Only update fragment count if there are scales after it for the vertex stage.
            if (fragmentCount != totalCount && fragmentCount != _fragmentScaleCount)
            {
                _fragmentScaleCount = fragmentCount;
                UpdateFragmentRenderScaleCount(_fragmentScaleCount);
            }

            if (changed)
            {
                UpdateRenderScale(_renderScale, 0, 1 + totalCount);
            }
        }

        public void SetRenderTargetIsBgra(int index, bool isBgra)
        {
            bool isBgraChanged = (_fpIsBgra[index].X != 0) != isBgra;

            if (isBgraChanged)
            {
                _fpIsBgra[index].X = isBgra ? 1 : 0;
                UpdateFragmentIsBgra(_fpIsBgra, index, 1);
            }
        }

        public void SetViewportTransformDisable(float viewportWidth, float viewportHeight, float scale, bool disableTransform)
        {
            float disableTransformF = disableTransform ? 1.0f : 0.0f;
            if (_data.ViewportInverse.W != disableTransformF || disableTransform)
            {
                UpdateViewportInverse(new Vector4<float>
                {
                    X = scale * 2f / viewportWidth,
                    Y = scale * 2f / viewportHeight,
                    Z = 1,
                    W = disableTransformF
                });
            }
        }

        public void Commit()
        {
            if (_startOffset != -1)
            {
                if (_handle == BufferHandle.Null)
                {
                    _handle = _renderer.CreateBuffer(SupportBuffer.RequiredSize);
                    _renderer.Pipeline.ClearBuffer(_handle, 0, SupportBuffer.RequiredSize, 0);

                    var range = new BufferRange(_handle, 0, SupportBuffer.RequiredSize);
                    _renderer.Pipeline.SetUniformBuffers(stackalloc[] { new BufferAssignment(0, range) });
                }

                ReadOnlySpan<byte> data = MemoryMarshal.Cast<SupportBuffer, byte>(MemoryMarshal.CreateSpan(ref _data, 1));

                _renderer.SetBufferData(_handle, _startOffset, data.Slice(_startOffset, _endOffset - _startOffset));

                _startOffset = -1;
                _endOffset = -1;
            }
        }

        public void Dispose()
        {
            if (_handle != BufferHandle.Null)
            {
                _renderer.DeleteBuffer(_handle);
                _handle = BufferHandle.Null;
            }
        }
    }
}
