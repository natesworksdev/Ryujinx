using Ryujinx.Common;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.StructuredIr;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Ryujinx.Graphics.Shader.Translation
{
    class ResourceManager
    {
        // Those values are used if the shader as local or shared memory access,
        // but for some reason the supplied size was 0.
        private const int DefaultLocalMemorySize = 128;
        private const int DefaultSharedMemorySize = 4096;

        private static readonly string[] _stagePrefixes = new string[] { "cp", "vp", "tcp", "tep", "gp", "fp" };

        private readonly IGpuAccessor _gpuAccessor;
        private readonly ShaderStage _stage;
        private readonly string _stagePrefix;

        private readonly int[] _cbSlotToBindingMap;
        private readonly int[] _sbSlotToBindingMap;
        private uint _sbSlotWritten;

        private readonly Dictionary<int, int> _sbSlots;

        private readonly HashSet<int> _usedConstantBufferBindings;

        private readonly record struct TextureInfo(int CbufSlot, int Handle, TextureFormat Format);

        private struct TextureMeta
        {
            public int Binding;
            public bool AccurateType;
            public SamplerType Type;
            public TextureUsageFlags UsageFlags;
        }

        private readonly Dictionary<TextureInfo, TextureMeta> _usedTextures;
        private readonly Dictionary<TextureInfo, TextureMeta> _usedImages;

        public int LocalMemoryId { get; private set; }
        public int SharedMemoryId { get; private set; }

        public int LocalVertexDataMemoryId { get; private set; }
        public int LocalTopologyRemapMemoryId { get; private set; }
        public int LocalVertexIndexVertexRateMemoryId { get; private set; }
        public int LocalVertexIndexInstanceRateMemoryId { get; private set; }
        public int LocalGeometryOutputVertexCountMemoryId { get; private set; }
        public int LocalGeometryOutputIndexCountMemoryId { get; private set; }

        public ShaderProperties Properties { get; }

        public ResourceReservations Reservations { get; }

        public ResourceManager(ShaderStage stage, IGpuAccessor gpuAccessor, ResourceReservations reservations = null)
        {
            _gpuAccessor = gpuAccessor;
            Properties = new();
            Reservations = reservations;
            _stage = stage;
            _stagePrefix = GetShaderStagePrefix(stage);

            _cbSlotToBindingMap = new int[18];
            _sbSlotToBindingMap = new int[16];
            _cbSlotToBindingMap.AsSpan().Fill(-1);
            _sbSlotToBindingMap.AsSpan().Fill(-1);

            _sbSlots = new();

            _usedConstantBufferBindings = new();

            _usedTextures = new();
            _usedImages = new();

            Properties.AddOrUpdateConstantBuffer(new(BufferLayout.Std140, 0, SupportBuffer.Binding, "support_buffer", SupportBuffer.GetStructureType()));

            LocalMemoryId = -1;
            SharedMemoryId = -1;
        }

        public void SetCurrentLocalMemory(int size, bool isUsed)
        {
            if (isUsed)
            {
                if (size <= 0)
                {
                    size = DefaultLocalMemorySize;
                }

                var lmem = new MemoryDefinition("local_memory", AggregateType.Array | AggregateType.U32, BitUtils.DivRoundUp(size, sizeof(uint)));

                LocalMemoryId = Properties.AddLocalMemory(lmem);
            }
            else
            {
                LocalMemoryId = -1;
            }
        }

        public void SetCurrentSharedMemory(int size, bool isUsed)
        {
            if (isUsed)
            {
                if (size <= 0)
                {
                    size = DefaultSharedMemorySize;
                }

                var smem = new MemoryDefinition("shared_memory", AggregateType.Array | AggregateType.U32, BitUtils.DivRoundUp(size, sizeof(uint)));

                SharedMemoryId = Properties.AddSharedMemory(smem);
            }
            else
            {
                SharedMemoryId = -1;
            }
        }

        public void SetVertexAsComputeLocalMemories(ShaderStage stage, InputTopology inputTopology)
        {
            LocalVertexDataMemoryId = AddMemoryDefinition("local_vertex_data", AggregateType.Array | AggregateType.FP32, Reservations.OutputSizePerInvocation);

            if (stage == ShaderStage.Vertex)
            {
                LocalVertexIndexVertexRateMemoryId = AddMemoryDefinition("local_vertex_index_vr", AggregateType.U32);
                LocalVertexIndexInstanceRateMemoryId = AddMemoryDefinition("local_vertex_index_ir", AggregateType.U32);
            }
            else if (stage == ShaderStage.Geometry)
            {
                LocalTopologyRemapMemoryId = AddMemoryDefinition("local_topology_remap", AggregateType.Array | AggregateType.U32, inputTopology.ToInputVertices());

                LocalGeometryOutputVertexCountMemoryId = AddMemoryDefinition("local_geometry_output_vertex", AggregateType.U32);
                LocalGeometryOutputIndexCountMemoryId = AddMemoryDefinition("local_geometry_output_index", AggregateType.U32);
            }
        }

        private int AddMemoryDefinition(string name, AggregateType type, int arrayLength = 1)
        {
            return Properties.AddLocalMemory(new MemoryDefinition(name, type, arrayLength));
        }

        public void EnsureBindlessBinding(TargetApi targetApi, SamplerType samplerType, bool isImage)
        {
            if (targetApi == TargetApi.Vulkan)
            {
                Properties.AddOrUpdateConstantBuffer(new BufferDefinition(
                    BufferLayout.Std140,
                    Constants.BindlessTextureSetIndex,
                    Constants.BindlessTableBinding,
                    "bindless_table",
                    new StructureType(new[] { new StructureField(AggregateType.Array | AggregateType.Vector2 | AggregateType.U32, "table", 0x1000) })));

                Properties.AddOrUpdateStorageBuffer(new BufferDefinition(
                    BufferLayout.Std430,
                    Constants.BindlessTextureSetIndex,
                    Constants.BindlessScalesBinding,
                    "bindless_scales",
                    new StructureType(new[] { new StructureField(AggregateType.Array | AggregateType.FP32, "scales", 0) })));

                if (isImage)
                {
                    string name = $"bindless_{samplerType.ToGlslImageType(AggregateType.FP32)}";

                    if (samplerType == SamplerType.TextureBuffer)
                    {
                        AddBindlessDefinition(8, 0, name, SamplerType.TextureBuffer);
                    }
                    else
                    {
                        AddBindlessDefinition(7, 0, name, samplerType);
                    }
                }
                else
                {
                    string name = $"bindless_{(samplerType & ~SamplerType.Shadow).ToGlslSamplerType()}";

                    if (samplerType == SamplerType.TextureBuffer)
                    {
                        AddBindlessSeparateDefinition(5, 0, name, SamplerType.TextureBuffer);
                    }
                    else
                    {
                        AddBindlessSeparateDefinition(4, 2, name, samplerType);
                    }

                    // Sampler
                    AddBindlessDefinition(6, 0, "bindless_samplers", SamplerType.None);
                }
            }
        }

        private void AddBindlessDefinition(int set, int binding, string name, SamplerType samplerType)
        {
            TextureDefinition definition = new(set, binding, name, samplerType, TextureFormat.Unknown, TextureUsageFlags.None, 0);

            Properties.AddOrUpdateTexture(definition, samplerType & ~(SamplerType.Separate | SamplerType.Shadow));
        }

        private void AddBindlessSeparateDefinition(int set, int binding, string name, SamplerType samplerType)
        {
            samplerType = (samplerType & ~SamplerType.Shadow) | SamplerType.Separate;

            AddBindlessDefinition(set, binding, name, samplerType);
        }

        public int GetConstantBufferBinding(int slot)
        {
            int binding = _cbSlotToBindingMap[slot];
            if (binding < 0)
            {
                binding = _gpuAccessor.QueryBindingConstantBuffer(slot);
                _cbSlotToBindingMap[slot] = binding;
                string slotNumber = slot.ToString(CultureInfo.InvariantCulture);

                AddNewConstantBuffer(binding, $"{_stagePrefix}_c{slotNumber}");
            }

            return SetBindingPair.Pack(Constants.VkConstantBufferSetIndex, binding);
        }

        public bool TryGetStorageBufferBinding(int sbCbSlot, int sbCbOffset, bool write, out int binding)
        {
            if (!TryGetSbSlot((byte)sbCbSlot, (ushort)sbCbOffset, out int slot))
            {
                binding = 0;

                return false;
            }

            binding = _sbSlotToBindingMap[slot];

            if (binding < 0)
            {
                binding = _gpuAccessor.QueryBindingStorageBuffer(slot);
                _sbSlotToBindingMap[slot] = binding;
                string slotNumber = slot.ToString(CultureInfo.InvariantCulture);

                AddNewStorageBuffer(binding, $"{_stagePrefix}_s{slotNumber}");
            }

            binding = SetBindingPair.Pack(Constants.VkStorageBufferSetIndex, binding);

            if (write)
            {
                _sbSlotWritten |= 1u << slot;
            }

            return true;
        }

        private bool TryGetSbSlot(byte sbCbSlot, ushort sbCbOffset, out int slot)
        {
            int key = PackSbCbInfo(sbCbSlot, sbCbOffset);

            if (!_sbSlots.TryGetValue(key, out slot))
            {
                slot = _sbSlots.Count;

                if (slot >= _sbSlotToBindingMap.Length)
                {
                    return false;
                }

                _sbSlots.Add(key, slot);
            }

            return true;
        }

        public bool TryGetConstantBufferSlot(int binding, out int slot)
        {
            for (slot = 0; slot < _cbSlotToBindingMap.Length; slot++)
            {
                if (_cbSlotToBindingMap[slot] < 0)
                {
                    continue;
                }

                if (SetBindingPair.Pack(Constants.VkConstantBufferSetIndex, _cbSlotToBindingMap[slot]) == binding)
                {
                    return true;
                }
            }

            slot = 0;
            return false;
        }

        public int GetTextureOrImageBinding(
            Instruction inst,
            SamplerType type,
            TextureFormat format,
            TextureFlags flags,
            int cbufSlot,
            int handle)
        {
            inst &= Instruction.Mask;
            bool isImage = inst == Instruction.ImageLoad || inst == Instruction.ImageStore || inst == Instruction.ImageAtomic;
            bool isWrite = inst == Instruction.ImageStore || inst == Instruction.ImageAtomic;
            bool accurateType = !inst.IsTextureQuery();
            bool intCoords = isImage || flags.HasFlag(TextureFlags.IntCoords) || inst == Instruction.TextureQuerySize;
            bool coherent = flags.HasFlag(TextureFlags.Coherent);

            if (!isImage)
            {
                format = TextureFormat.Unknown;
            }

            int binding = GetTextureOrImageBinding(cbufSlot, handle, type, format, isImage, intCoords, isWrite, accurateType, coherent);

            _gpuAccessor.RegisterTexture(handle, cbufSlot);

            return SetBindingPair.Pack(isImage ? Constants.VkImageSetIndex : Constants.VkTextureSetIndex, binding);
        }

        private int GetTextureOrImageBinding(
            int cbufSlot,
            int handle,
            SamplerType type,
            TextureFormat format,
            bool isImage,
            bool intCoords,
            bool write,
            bool accurateType,
            bool coherent)
        {
            var dimensions = type.GetDimensions();
            var dict = isImage ? _usedImages : _usedTextures;

            var usageFlags = TextureUsageFlags.None;

            if (intCoords)
            {
                usageFlags |= TextureUsageFlags.NeedsScaleValue;

                var canScale = _stage.SupportsRenderScale() && !write && dimensions == 2;

                if (!canScale)
                {
                    // Resolution scaling cannot be applied to this texture right now.
                    // Flag so that we know to blacklist scaling on related textures when binding them.
                    usageFlags |= TextureUsageFlags.ResScaleUnsupported;
                }
            }

            if (write)
            {
                usageFlags |= TextureUsageFlags.ImageStore;
            }

            if (coherent)
            {
                usageFlags |= TextureUsageFlags.ImageCoherent;
            }

            var info = new TextureInfo(cbufSlot, handle, format);
            var meta = new TextureMeta()
            {
                AccurateType = accurateType,
                Type = type,
                UsageFlags = usageFlags
            };

            int binding;

            if (dict.TryGetValue(info, out var existingMeta))
            {
                dict[info] = MergeTextureMeta(meta, existingMeta);
                binding = existingMeta.Binding;
            }
            else
            {
                bool isBuffer = (type & SamplerType.Mask) == SamplerType.TextureBuffer;

                binding = isImage
                    ? _gpuAccessor.QueryBindingImage(dict.Count, isBuffer)
                    : _gpuAccessor.QueryBindingTexture(dict.Count, isBuffer);

                meta.Binding = binding;

                dict.Add(info, meta);
            }

            string nameSuffix;

            if (isImage)
            {
                nameSuffix = cbufSlot < 0
                    ? $"i_tcb_{handle:X}_{format.ToGlslFormat()}"
                    : $"i_cb{cbufSlot}_{handle:X}_{format.ToGlslFormat()}";
            }
            else
            {
                nameSuffix = cbufSlot < 0 ? $"t_tcb_{handle:X}" : $"t_cb{cbufSlot}_{handle:X}";
            }

            var definition = new TextureDefinition(
                isImage ? 3 : 2,
                binding,
                $"{_stagePrefix}_{nameSuffix}",
                meta.Type,
                info.Format,
                meta.UsageFlags);

            if (isImage)
            {
                Properties.AddOrUpdateImage(definition);
            }
            else
            {
                Properties.AddOrUpdateTexture(definition);
            }

            return binding;
        }

        private static TextureMeta MergeTextureMeta(TextureMeta meta, TextureMeta existingMeta)
        {
            meta.Binding = existingMeta.Binding;
            meta.UsageFlags |= existingMeta.UsageFlags;

            // If the texture we have has inaccurate type information, then
            // we prefer the most accurate one.
            if (existingMeta.AccurateType)
            {
                meta.AccurateType = true;
                meta.Type = existingMeta.Type;
            }

            return meta;
        }

        public void SetUsageFlagsForTextureQuery(int binding, SamplerType type)
        {
            TextureInfo selectedInfo = default;
            TextureMeta selectedMeta = default;
            bool found = false;

            foreach ((TextureInfo info, TextureMeta meta) in _usedTextures)
            {
                if (SetBindingPair.Pack(Constants.VkTextureSetIndex, meta.Binding) == binding)
                {
                    selectedInfo = info;
                    selectedMeta = meta;
                    found = true;
                    break;
                }
            }

            if (found)
            {
                selectedMeta.UsageFlags |= TextureUsageFlags.NeedsScaleValue;

                var dimensions = type.GetDimensions();
                var canScale = _stage.SupportsRenderScale() && dimensions == 2;

                if (!canScale)
                {
                    // Resolution scaling cannot be applied to this texture right now.
                    // Flag so that we know to blacklist scaling on related textures when binding them.
                    selectedMeta.UsageFlags |= TextureUsageFlags.ResScaleUnsupported;
                }

                _usedTextures[selectedInfo] = selectedMeta;
            }
        }

        public void SetUsedConstantBufferBinding(int binding)
        {
            _usedConstantBufferBindings.Add(binding);
        }

        public BufferDescriptor[] GetConstantBufferDescriptors()
        {
            var descriptors = new BufferDescriptor[_usedConstantBufferBindings.Count];

            int descriptorIndex = 0;

            for (int slot = 0; slot < _cbSlotToBindingMap.Length; slot++)
            {
                int binding = _cbSlotToBindingMap[slot];

                if (binding >= 0 && _usedConstantBufferBindings.Contains(SetBindingPair.Pack(Constants.VkConstantBufferSetIndex, binding)))
                {
                    descriptors[descriptorIndex++] = new BufferDescriptor(binding, slot);
                }
            }

            if (descriptors.Length != descriptorIndex)
            {
                Array.Resize(ref descriptors, descriptorIndex);
            }

            return descriptors;
        }

        public BufferDescriptor[] GetStorageBufferDescriptors()
        {
            var descriptors = new BufferDescriptor[_sbSlots.Count];

            int descriptorIndex = 0;

            foreach ((int key, int slot) in _sbSlots)
            {
                int binding = _sbSlotToBindingMap[slot];

                if (binding >= 0)
                {
                    (int sbCbSlot, int sbCbOffset) = UnpackSbCbInfo(key);
                    BufferUsageFlags flags = (_sbSlotWritten & (1u << slot)) != 0 ? BufferUsageFlags.Write : BufferUsageFlags.None;
                    descriptors[descriptorIndex++] = new BufferDescriptor(binding, slot, sbCbSlot, sbCbOffset, flags);
                }
            }

            if (descriptors.Length != descriptorIndex)
            {
                Array.Resize(ref descriptors, descriptorIndex);
            }

            return descriptors;
        }

        public TextureDescriptor[] GetTextureDescriptors()
        {
            return GetDescriptors(_usedTextures, _usedTextures.Count);
        }

        public TextureDescriptor[] GetImageDescriptors()
        {
            return GetDescriptors(_usedImages, _usedImages.Count);
        }

        private static TextureDescriptor[] GetDescriptors(IReadOnlyDictionary<TextureInfo, TextureMeta> usedResources, int count)
        {
            TextureDescriptor[] descriptors = new TextureDescriptor[count];

            int descriptorIndex = 0;

            foreach ((TextureInfo info, TextureMeta meta) in usedResources)
            {
                descriptors[descriptorIndex++] = new TextureDescriptor(
                    meta.Binding,
                    meta.Type,
                    info.Format,
                    info.CbufSlot,
                    info.Handle,
                    meta.UsageFlags);
            }

            return descriptors;
        }

        public bool TryGetCbufSlotAndHandleForTexture(int binding, out int cbufSlot, out int handle)
        {
            foreach ((TextureInfo info, TextureMeta meta) in _usedTextures)
            {
                if (SetBindingPair.Pack(Constants.VkTextureSetIndex, meta.Binding) == binding)
                {
                    cbufSlot = info.CbufSlot;
                    handle = info.Handle;

                    return true;
                }
            }

            cbufSlot = 0;
            handle = 0;
            return false;
        }

        private static int FindDescriptorIndex(TextureDescriptor[] array, int setIndex, int binding)
        {
            return Array.FindIndex(array, x => SetBindingPair.Pack(setIndex, x.Binding) == binding);
        }

        public int FindTextureDescriptorIndex(int binding)
        {
            return FindDescriptorIndex(GetTextureDescriptors(), Constants.VkTextureSetIndex, binding);
        }

        public int FindImageDescriptorIndex(int binding)
        {
            return FindDescriptorIndex(GetImageDescriptors(), Constants.VkImageSetIndex, binding);
        }

        private void AddNewConstantBuffer(int binding, string name)
        {
            StructureType type = new(new[]
            {
                new StructureField(AggregateType.Array | AggregateType.Vector4 | AggregateType.FP32, "data", Constants.ConstantBufferSize / 16),
            });

            Properties.AddOrUpdateConstantBuffer(new(BufferLayout.Std140, 0, binding, name, type));
        }

        private void AddNewStorageBuffer(int binding, string name)
        {
            StructureType type = new(new[]
            {
                new StructureField(AggregateType.Array | AggregateType.U32, "data", 0),
            });

            Properties.AddOrUpdateStorageBuffer(new(BufferLayout.Std430, 1, binding, name, type));
        }

        public static string GetShaderStagePrefix(ShaderStage stage)
        {
            uint index = (uint)stage;

            return index >= _stagePrefixes.Length ? "invalid" : _stagePrefixes[index];
        }

        private static int PackSbCbInfo(int sbCbSlot, int sbCbOffset)
        {
            return sbCbOffset | (sbCbSlot << 16);
        }

        private static (int, int) UnpackSbCbInfo(int key)
        {
            return ((byte)(key >> 16), (ushort)key);
        }
    }
}
