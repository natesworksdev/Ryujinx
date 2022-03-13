using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Shader
{
    class ShaderSpecializationState
    {
        private const uint ComsMagic = (byte)'C' | ((byte)'O' << 8) | ((byte)'M' << 16) | ((byte)'S' << 24);
        private const uint GfxsMagic = (byte)'G' | ((byte)'F' << 8) | ((byte)'X' << 16) | ((byte)'S' << 24);
        private const uint TfbdMagic = (byte)'T' | ((byte)'F' << 8) | ((byte)'B' << 16) | ((byte)'D' << 24);
        private const uint TexkMagic = (byte)'T' | ((byte)'E' << 8) | ((byte)'X' << 16) | ((byte)'K' << 24);
        private const uint TexsMagic = (byte)'T' | ((byte)'E' << 8) | ((byte)'X' << 16) | ((byte)'S' << 24);

        private enum QueriedStateFlags : byte
        {
            EarlyZForce = 1 << 0,
            PrimitiveTopology = 1 << 1,
            TessellationMode = 1 << 2,
            ConstantBufferUse = 1 << 3,
            TransformFeedback = 1 << 4
        }

        private QueriedStateFlags _queriedState;
        private bool _compute;

        public GpuChannelComputeState ComputeState;
        public GpuChannelGraphicsState GraphicsState;
        public uint ConstantBufferUse;

        public TransformFeedbackDescriptor[] TransformFeedbackDescriptors;

        private enum QueriedTextureStateFlags : byte
        {
            TextureFormat = 1 << 0,
            SamplerType = 1 << 1,
            CoordNormalized = 1 << 2
        }

        private class Box<T>
        {
            public T Value;
        }

        private struct TextureSpecializationState
        {
            public QueriedTextureStateFlags QueriedFlags;
            public uint Format;
            public bool FormatSrgb;
            public Image.TextureTarget TextureTarget;
            public bool CoordNormalized;
        }

        private struct TextureKey : IEquatable<TextureKey>
        {
            public readonly int StageIndex;
            public readonly int Handle;
            public readonly int CbufSlot;

            public TextureKey(int stageIndex, int handle, int cbufSlot)
            {
                StageIndex = stageIndex;
                Handle = handle;
                CbufSlot = cbufSlot;
            }

            public override bool Equals(object obj)
            {
                return obj is TextureKey textureKey && Equals(textureKey);
            }

            public bool Equals(TextureKey other)
            {
                return StageIndex == other.StageIndex && Handle == other.Handle && CbufSlot == other.CbufSlot;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(StageIndex, Handle, CbufSlot);
            }
        }

        private readonly Dictionary<TextureKey, Box<TextureSpecializationState>> _textureSpecialization;

        private ShaderSpecializationState()
        {
            _textureSpecialization = new Dictionary<TextureKey, Box<TextureSpecializationState>>();
        }

        public ShaderSpecializationState(GpuChannelComputeState state) : this()
        {
            ComputeState = state;
            _compute = true;
        }

        public ShaderSpecializationState(GpuChannelGraphicsState state, TransformFeedbackDescriptor[] descriptors) : this()
        {
            GraphicsState = state;
            _compute = false;

            if (descriptors != null)
            {
                TransformFeedbackDescriptors = descriptors;
                _queriedState |= QueriedStateFlags.TransformFeedback;
            }
        }

        public void RecordEarlyZForce()
        {
            _queriedState |= QueriedStateFlags.EarlyZForce;
        }

        public void RecordPrimitiveTopology()
        {
            _queriedState |= QueriedStateFlags.PrimitiveTopology;
        }

        public void RecordTessellationMode()
        {
            _queriedState |= QueriedStateFlags.TessellationMode;
        }

        public void RecordConstantBufferUse(uint useMask)
        {
            ConstantBufferUse = useMask;
            _queriedState |= QueriedStateFlags.ConstantBufferUse;
        }

        public void RegisterTexture(int stageIndex, int handle, int cbufSlot, Image.TextureDescriptor descriptor)
        {
            Box<TextureSpecializationState> state = GetOrCreateTextureSpecState(stageIndex, handle, cbufSlot);
            state.Value.Format = descriptor.UnpackFormat();
            state.Value.FormatSrgb = descriptor.UnpackSrgb();
            state.Value.TextureTarget = descriptor.UnpackTextureTarget();
            state.Value.CoordNormalized = descriptor.UnpackTextureCoordNormalized();
        }

        public void RecordTextureFormat(int stageIndex, int handle, int cbufSlot)
        {
            Box<TextureSpecializationState> state = GetOrCreateTextureSpecState(stageIndex, handle, cbufSlot);
            state.Value.QueriedFlags |= QueriedTextureStateFlags.TextureFormat;
        }

        public void RecordTextureSamplerType(int stageIndex, int handle, int cbufSlot)
        {
            Box<TextureSpecializationState> state = GetOrCreateTextureSpecState(stageIndex, handle, cbufSlot);
            state.Value.QueriedFlags |= QueriedTextureStateFlags.SamplerType;
        }

        public void RecordTextureCoordNormalized(int stageIndex, int handle, int cbufSlot)
        {
            Box<TextureSpecializationState> state = GetOrCreateTextureSpecState(stageIndex, handle, cbufSlot);
            state.Value.QueriedFlags |= QueriedTextureStateFlags.CoordNormalized;
        }

        public (uint, bool) GetFormat(int stageIndex, int handle, int cbufSlot)
        {
            TextureSpecializationState state = GetTextureSpecState(stageIndex, handle, cbufSlot).Value;
            return (state.Format, state.FormatSrgb);
        }

        public Image.TextureTarget GetTextureTarget(int stageIndex, int handle, int cbufSlot)
        {
            return GetTextureSpecState(stageIndex, handle, cbufSlot).Value.TextureTarget;
        }

        public bool GetCoordNormalized(int stageIndex, int handle, int cbufSlot)
        {
            return GetTextureSpecState(stageIndex, handle, cbufSlot).Value.CoordNormalized;
        }

        private Box<TextureSpecializationState> GetOrCreateTextureSpecState(int stageIndex, int handle, int cbufSlot)
        {
            TextureKey key = new TextureKey(stageIndex, handle, cbufSlot);

            if (!_textureSpecialization.TryGetValue(key, out Box<TextureSpecializationState> state))
            {
                _textureSpecialization.Add(key, state = new Box<TextureSpecializationState>());
            }

            return state;
        }

        private Box<TextureSpecializationState> GetTextureSpecState(int stageIndex, int handle, int cbufSlot)
        {
            TextureKey key = new TextureKey(stageIndex, handle, cbufSlot);

            if (_textureSpecialization.TryGetValue(key, out Box<TextureSpecializationState> state))
            {
                return state;
            }

            return null;
        }

        public bool MatchesGraphics(GpuChannel channel, GpuChannelPoolState poolState)
        {
            return Matches(channel, poolState, isCompute: false);
        }

        public bool MatchesCompute(GpuChannel channel, GpuChannelPoolState poolState)
        {
            return Matches(channel, poolState, isCompute: true);
        }

        private bool Matches(GpuChannel channel, GpuChannelPoolState poolState, bool isCompute)
        {

            foreach (var kv in _textureSpecialization)
            {
                TextureKey textureKey = kv.Key;

                bool cbAccessible = isCompute
                    ? channel.BufferManager.GetComputeUniformBufferAddress(poolState.TextureBufferIndex) != 0
                    : channel.BufferManager.GetGraphicsUniformBufferAddress(textureKey.StageIndex, poolState.TextureBufferIndex) != 0;

                if (!cbAccessible)
                {
                    continue;
                }

                Image.TextureDescriptor descriptor;

                if (isCompute)
                {
                    descriptor = channel.TextureManager.GetComputeTextureDescriptor(
                        poolState.TexturePoolGpuVa,
                        poolState.TextureBufferIndex,
                        poolState.TexturePoolMaximumId,
                        textureKey.Handle,
                        textureKey.CbufSlot);
                }
                else
                {
                    descriptor = channel.TextureManager.GetGraphicsTextureDescriptor(
                        poolState.TexturePoolGpuVa,
                        poolState.TextureBufferIndex,
                        poolState.TexturePoolMaximumId,
                        textureKey.StageIndex,
                        textureKey.Handle,
                        textureKey.CbufSlot);
                }

                Box<TextureSpecializationState> specializationState = kv.Value;

                if (specializationState.Value.QueriedFlags.HasFlag(QueriedTextureStateFlags.CoordNormalized) &&
                    specializationState.Value.CoordNormalized != descriptor.UnpackTextureCoordNormalized())
                {
                    return false;
                }
            }

            return true;
        }

        public static ShaderSpecializationState Read(ref BinarySerialization dataReader)
        {
            ShaderSpecializationState specState = new ShaderSpecializationState();

            dataReader.TryRead(ref specState._queriedState);
            dataReader.TryRead(ref specState._compute);

            if (specState._compute)
            {
                dataReader.ReadWithMagicAndSize(ref specState.ComputeState, ComsMagic);
            }
            else
            {
                dataReader.ReadWithMagicAndSize(ref specState.GraphicsState, GfxsMagic);
            }

            if (specState._queriedState.HasFlag(QueriedStateFlags.ConstantBufferUse))
            {
                dataReader.TryRead(ref specState.ConstantBufferUse);
            }

            if (specState._queriedState.HasFlag(QueriedStateFlags.TransformFeedback))
            {
                ushort tfCount = 0;
                dataReader.TryRead(ref tfCount);
                specState.TransformFeedbackDescriptors = new TransformFeedbackDescriptor[tfCount];

                for (int index = 0; index < tfCount; index++)
                {
                    dataReader.ReadWithMagicAndSize(ref specState.TransformFeedbackDescriptors[index], TfbdMagic);
                }
            }

            ushort count = 0;
            dataReader.TryRead(ref count);

            for (int index = 0; index < count; index++)
            {
                TextureKey textureKey = default;
                Box<TextureSpecializationState> textureState = new Box<TextureSpecializationState>();

                dataReader.ReadWithMagicAndSize(ref textureKey, TexkMagic);
                dataReader.ReadWithMagicAndSize(ref textureState.Value, TexsMagic);

                specState._textureSpecialization[textureKey] = textureState;
            }

            return specState;
        }

        public void Write(ref BinarySerialization dataWriter)
        {
            dataWriter.Write(ref _queriedState);
            dataWriter.Write(ref _compute);

            if (_compute)
            {
                dataWriter.WriteWithMagicAndSize(ref ComputeState, ComsMagic);
            }
            else
            {
                dataWriter.WriteWithMagicAndSize(ref GraphicsState, GfxsMagic);
            }

            if (_queriedState.HasFlag(QueriedStateFlags.ConstantBufferUse))
            {
                dataWriter.Write(ref ConstantBufferUse);
            }

            if (_queriedState.HasFlag(QueriedStateFlags.TransformFeedback))
            {
                ushort tfCount = (ushort)TransformFeedbackDescriptors.Length;
                dataWriter.Write(ref tfCount);

                for (int index = 0; index < TransformFeedbackDescriptors.Length; index++)
                {
                    dataWriter.WriteWithMagicAndSize(ref TransformFeedbackDescriptors[index], TfbdMagic);
                }
            }

            ushort count = (ushort)_textureSpecialization.Count;
            dataWriter.Write(ref count);

            foreach (var kv in _textureSpecialization)
            {
                var textureKey = kv.Key;
                var textureState = kv.Value;

                dataWriter.WriteWithMagicAndSize(ref textureKey, TexkMagic);
                dataWriter.WriteWithMagicAndSize(ref textureState.Value, TexsMagic);
            }
        }
    }
}